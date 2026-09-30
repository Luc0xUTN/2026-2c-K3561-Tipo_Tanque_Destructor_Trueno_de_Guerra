using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP;

// Responsabilidad: Administrar el campo de batalla (Terrain, los carriles/claros
// navegables, y Forest). Pipeline: A) spawns + zona media -> B) carriles (datos
// geométricos, nunca se dibujan) -> C) Forest llena el complemento de los carriles.
public class Battlefield
{
    // Seed madre temporal (mismo patrón que el flag de debug de Terrain).
    // TODO: en algún momento esto va a ser configurable (menú / seed aleatoria
    // real por partida) en vez de una constante fija de debug.
    private const int MasterSeed = 12345;

    // --- A) Spawns y zona media ---
    private const float SpawnRadius = 126f; // R: distancia del centro del mapa a cada spawn.
    private const float SpawnClearingRadius = 22f; // claro navegable alrededor de cada spawn (~6 tanques).
    private const float CenterClearingRadius = 30f; // claro navegable del centro, más grande a propósito (fuego cruzado).

    // --- B) Carriles ---
    private const int LaneCount = 3;
    private const int LaneNiveles = 9; // provisorio, a tunear mirando el resultado.
    private const float LaneDesviacionRelativa = 0.21f; // provisorio — ruido orgánico fino, ya no es lo único que separa los carriles.
    private const float LaneHalfWidth = 7f; // ancho navegable fijo por ahora (la versión con bordes independientes y ancho variable queda para una iteración posterior).

    private readonly string _shaderRoute;
    private readonly Color _terrainColor;

    private Terrain _terrain;
    private Forest _forest;

    private readonly List<List<Vector2>> _laneCurves = new();
    // halfA+halfB concatenados por carril (3 curvas completas spawnA->spawnB en vez
    // de las 6 mitades de _laneCurves) — las usa AI para elegir "la lane más cercana"
    // como una sola curva continua, no como dos mitades separadas.
    private readonly List<List<Vector2>> _fullLaneCurves = new();
    private Vector2 _spawnA;
    private Vector2 _spawnB;
    private Vector2 _center;

    public Battlefield(string shaderRoute, Color terrainColor)
    {
        _shaderRoute = shaderRoute;
        _terrainColor = terrainColor;
    }

    public void Initialize(GraphicsDevice device, float mapSize)
    {
        // Cada módulo deriva su propia seed independiente a partir de la
        // MasterSeed, en vez de compartir un único stream secuencial: así,
        // cambios internos en un módulo no corren silenciosamente los resultados
        // de otro para la misma seed.
        var terrainSeed = MasterSeed;
        var forestSeed = unchecked(MasterSeed * -1640531527); // mix multiplicativo tipo Knuth
        var lanesSeed = unchecked(MasterSeed * 668265263);

        _terrain = new Terrain(_shaderRoute, _terrainColor);
        _terrain.Initialize(device, mapSize, terrainSeed);

        GenerateSpawnsAndLanes(lanesSeed);

        // Terrain y los carriles ya están listos: Forest consulta la altura real
        // (GetHeightAt) y qué zonas son navegables (IsNavigable) para llenar
        // únicamente el complemento de los carriles con árboles/rocas.
        _forest = new Forest();
        _forest.Initialize(new Vector2(mapSize, mapSize), forestSeed, _terrain.GetHeightAt, IsNavigable);
    }

    private void GenerateSpawnsAndLanes(int lanesSeed)
    {
        var random = new Random(lanesSeed);

        // A) Ángulo del eje spawn-spawn: varía de partida en partida, pero los dos
        // spawns siempre quedan diametralmente opuestos sobre ese eje.
        var angle = (float)(random.NextDouble() * MathHelper.TwoPi);
        var axis = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
        _spawnA = axis * SpawnRadius;
        _spawnB = -axis * SpawnRadius;
        _center = Vector2.Zero;

        // B) Los 3 carriles arrancan con un arco BASE distinto cada uno (determinístico,
        // no aleatorio) para que se separen entre sí desde el principio, en vez de
        // arrancar los tres pegados a la misma recta y confiar solo en el ruido fino
        // para diferenciarlos (esto fue justamente lo que no funcionó en el intento
        // anterior). Fan simétrico: -1, 0, +1 (el carril del medio queda más directo,
        // los de flanco se curvan hacia cada lado — coherente con la idea de carril
        // central vs. flancos de la investigación de World of Tanks).
        const float maxArcBase = 0.85f;
        for (var lane = 0; lane < LaneCount; lane++)
        {
            var fan = LaneCount == 1 ? 0f : (lane / (float)(LaneCount - 1)) * 2f - 1f; // -1..+1
            var arcBase = fan * maxArcBase;

            // Orden de sorteo fijo (mitad A y después mitad B) para que la misma seed
            // reproduzca siempre los mismos carriles.
            var seedHalfA = random.Next();
            var seedHalfB = random.Next();

            var halfA = Lane.GenerateCurve(_spawnA, _center, LaneNiveles, LaneDesviacionRelativa, arcBase, new Random(seedHalfA));
            var halfB = Lane.GenerateCurve(_center, _spawnB, LaneNiveles, LaneDesviacionRelativa, -arcBase, new Random(seedHalfB));

            _laneCurves.Add(halfA);
            _laneCurves.Add(halfB);

            var fullCurve = new List<Vector2>(halfA);
            fullCurve.AddRange(halfB);
            _fullLaneCurves.Add(fullCurve);
        }
    }

    // C) Consulta usada por Forest para no poner props sobre carriles ni claros.
    public bool IsNavigable(float x, float z)
    {
        var point = new Vector2(x, z);

        if (Vector2.Distance(point, _spawnA) <= SpawnClearingRadius) return true;
        if (Vector2.Distance(point, _spawnB) <= SpawnClearingRadius) return true;
        if (Vector2.Distance(point, _center) <= CenterClearingRadius) return true;

        foreach (var curve in _laneCurves)
        {
            if (Lane.DistanceToPolyline(point, curve) <= LaneHalfWidth)
                return true;
        }

        return false;
    }

    public void LoadContent(ContentManager content, string contentFolder3D)
    {
        _terrain.LoadContent(content);
        _forest.LoadContent(content, contentFolder3D, _shaderRoute);
    }

    public void Draw(GraphicsDevice device, Matrix view, Matrix projection)
    {
        _terrain.Draw(device, view, projection);
        _forest.Draw(device, view, projection);
    }

    // Única forma en que el resto del juego (spawn de tanque, más adelante IA)
    // puede consultar altura del terreno — no tienen que tocar Terrain directo.
    public float GetHeightAt(float x, float z)
    {
        return _terrain.GetHeightAt(x, z);
    }

    // Punto de spawn del jugador (siempre A) — no expone _spawnA directo, mismo
    // patrón que GetHeightAt/IsNavigable: nadie fuera de Battlefield toca los
    // datos internos, todo pasa por un método público de la fachada.
    public Vector2 GetSpawnA()
    {
        return _spawnA;
    }

    // Punto de spawn de los enemigos (siempre B, el opuesto al jugador).
    public Vector2 GetSpawnB()
    {
        return _spawnB;
    }

    // Las 3 lanes completas (spawnA->centro->spawnB), para que AI pueda elegir
    // "la más cercana" y perseguir al jugador sin salirse de zona navegable.
    public IReadOnlyList<List<Vector2>> GetLaneCurves()
    {
        return _fullLaneCurves;
    }
}
