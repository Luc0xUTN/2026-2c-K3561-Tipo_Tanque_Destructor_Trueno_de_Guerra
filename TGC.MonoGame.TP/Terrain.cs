using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP;

// Responsabilidad: Representar al terreno donde se va a situar la batalla
public class Terrain
{
    // Parámetros del heightmap procedural (ruido de Perlin + fBm).
    // Derivación completa de cada valor, incluida la justificación contra
    // el tamaño real medido del tanque y los límites de performance sin
    // culling: repo de contexto, progress/04-procedural-terrain-noise.md.
    private const int GridResolution = 128; // vértices por lado. Celda ≈ mapSize/127 ≈ 1/4 del largo del tanque.
    private const float HeightRange = 9f; // unidades de mundo, rango total (min a max). ≈3x el alto real estimado del tanque.
    private const float BaseFrequency = 0.0233f; // ≈ 4 colinas grandes a lo largo de un mapa de 300 unidades.
    private const int Octaves = 4;
    private const float Persistence = 0.55f;
    private const float Lacunarity = 1f;

    private const int PermutationSize = 256;
    private const int PermutationMask = PermutationSize - 1; // 255: la máscara tiene que ser potencia de 2 menos 1 (ver progress/04).

    // Debug: poner en true para que, la próxima vez que se genere el terreno, se
    // guarde una imagen en escala de grises con las alturas crudas
    // (heightmap_debug.png). Con "dotnet run" el archivo queda en la raíz del
    // proyecto (TGC.MonoGame.TP/), no en bin/Debug — está en .gitignore.
    // Sirve para verificar la FORMA del ruido (cantidad de colinas, que no se
    // vea ruidoso) de forma independiente de la cámara/escena.
    // OJO 1: la imagen se normaliza con el mínimo/máximo real generado, así
    // que siempre usa el contraste completo (blanco a negro) sin importar
    // cuán "sutil" sea HeightRange en términos absolutos — no reemplaza
    // probarlo in-game a la escala real del tanque.
    // OJO 2: mirarla en su tamaño real (128x128) o agrandada con un
    // resampleo suave (LANCZOS/bicúbico) — agrandada con "nearest neighbor"
    // o a tamaño miniatura, la textura fina de las octavas 1-2 se puede
    // confundir con ruido aunque la forma de base sea suave (pasó de verdad
    // el 2026-09-27 al revisar esto: no era un bug, era cómo se veía).
    private static readonly bool ExportHeightmapDebugImage = false;

    private Color _color { get; set; }

    private VertexBuffer _vertices;
    private IndexBuffer _indices;
    private Effect _effect;
    private Matrix _world;

    private string _shaderRoute;

    private float _size;
    private float[,] _heights; // copia en CPU, aparte de la GPU, para poder consultar altura (GetHeightAt).

    private int[] _permutation; // tamaño PermutationSize, sin duplicar: se enmascara en los tres puntos del hash, no hace falta.
    private Vector2[] _gradients; // PermutationSize direcciones unitarias equiespaciadas, sin sesgo de módulo.
    private float[] _octaveOffsetX;
    private float[] _octaveOffsetZ;

    public Terrain(string shaderRoute, Color color)
    {
        _shaderRoute = shaderRoute;
        _color = color;
    }

    public void LoadContent(ContentManager content)
    {
        _effect = content.Load<Effect>(_shaderRoute);
    }

    public void Initialize(GraphicsDevice device, float size, int seed)
    {
        _world = Matrix.Identity;
        _size = size;

        BuildNoiseTables(seed);
        BuildHeightmap(device);
    }

    private void BuildNoiseTables(int seed)
    {
        var state = unchecked((uint)seed);

        // Tabla de permutación: shuffle de 0..255, determinístico a partir de la seed.
        _permutation = new int[PermutationSize];
        for (var i = 0; i < PermutationSize; i++)
            _permutation[i] = i;

        for (var i = PermutationSize - 1; i > 0; i--)
        {
            var j = (int)(SplitMix32(ref state) % (uint)(i + 1));
            (_permutation[i], _permutation[j]) = (_permutation[j], _permutation[i]);
        }

        // Direcciones de gradiente: PermutationSize vectores unitarios equiespaciados.
        // Se descartó el switch de 8 direcciones con sesgo de módulo (ver progress/04):
        // acá no aplica el argumento de costo porque la generación es de carga única,
        // no por frame.
        _gradients = new Vector2[PermutationSize];
        for (var i = 0; i < PermutationSize; i++)
        {
            var angle = MathHelper.TwoPi * i / PermutationSize;
            _gradients[i] = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
        }

        // Offset por octava, derivado de la misma seed: evita que todas las octavas
        // colapsen a 0 simultáneamente en el origen (Perlin vale 0 en todo vértice
        // entero de su grilla, para cualquier frecuencia).
        _octaveOffsetX = new float[Octaves];
        _octaveOffsetZ = new float[Octaves];
        for (var i = 0; i < Octaves; i++)
        {
            _octaveOffsetX[i] = (SplitMix32(ref state) & 0xFFFF) / 65536f * PermutationSize;
            _octaveOffsetZ[i] = (SplitMix32(ref state) & 0xFFFF) / 65536f * PermutationSize;
        }
    }

    private static uint SplitMix32(ref uint state)
    {
        state += 0x9E3779B9;
        var z = state;
        z = (z ^ (z >> 16)) * 0x21F0AAAD;
        z = (z ^ (z >> 15)) * 0x735A2D97;
        return z ^ (z >> 15);
    }

    // hash(X, Z) = perm[(perm[X & m] + (Z & m)) & m], con m = PermutationMask.
    private int Hash(int x, int z)
    {
        var xi = x & PermutationMask;
        var zi = z & PermutationMask;
        return _permutation[(_permutation[xi] + zi) & PermutationMask];
    }

    private float Noise(float x, float z)
    {
        var x0 = (int)MathF.Floor(x);
        var z0 = (int)MathF.Floor(z);
        var x1 = x0 + 1;
        var z1 = z0 + 1;

        var n00 = Vector2.Dot(_gradients[Hash(x0, z0)], new Vector2(x - x0, z - z0));
        var n10 = Vector2.Dot(_gradients[Hash(x1, z0)], new Vector2(x - x1, z - z0));
        var n01 = Vector2.Dot(_gradients[Hash(x0, z1)], new Vector2(x - x0, z - z1));
        var n11 = Vector2.Dot(_gradients[Hash(x1, z1)], new Vector2(x - x1, z - z1));

        var u = Fade(x - x0);
        var v = Fade(z - z0);

        var nx0 = MathHelper.Lerp(n00, n10, u);
        var nx1 = MathHelper.Lerp(n01, n11, u);

        return MathHelper.Lerp(nx0, nx1, v);
    }

    // f(t) = 3t² - 2t³. f'(0) = f'(1) = 0: por eso no se nota la costura entre celdas
    // vecinas (ver progress/04 para la derivación completa).
    private static float Fade(float t) => t * t * (3f - 2f * t);

    // Loop de fBm: acumula freq (x lacunarity) y amp (x persistence) por octava,
    // normaliza dividiendo por la suma de amplitudes usada, no por una fórmula
    // cerrada precalculada.
    private float FractalNoise(float worldX, float worldZ)
    {
        var freq = BaseFrequency;
        var amp = 1f;
        float sum = 0f, norm = 0f;

        for (var octave = 0; octave < Octaves; octave++)
        {
            var sample = Noise(worldX * freq + _octaveOffsetX[octave], worldZ * freq + _octaveOffsetZ[octave]);
            sum += amp * sample;
            norm += amp;

            freq *= Lacunarity;
            amp *= Persistence;
        }

        return sum / norm; // rango aproximado [-1, 1]
    }

    private void BuildHeightmap(GraphicsDevice device)
    {
        _heights = new float[GridResolution, GridResolution];

        var vertices = new VertexPosition[GridResolution * GridResolution];
        var half = _size / 2f;
        var step = _size / (GridResolution - 1);

        for (var zIndex = 0; zIndex < GridResolution; zIndex++)
        {
            for (var xIndex = 0; xIndex < GridResolution; xIndex++)
            {
                var worldX = -half + xIndex * step;
                var worldZ = -half + zIndex * step;

                var height = FractalNoise(worldX, worldZ) * (HeightRange / 2f);

                _heights[xIndex, zIndex] = height;
                vertices[zIndex * GridResolution + xIndex] = new VertexPosition(new Vector3(worldX, height, worldZ));
            }
        }

        _vertices = new VertexBuffer(device, VertexPosition.VertexDeclaration, vertices.Length, BufferUsage.WriteOnly);
        _vertices.SetData(vertices);

        // Dos triángulos por celda, mismo esquema que la Unidad 7 (pág. 9):
        // primero {Xi,Zi} {Xi,Zi+1} {Xi+1,Zi+1}, segundo {Xi,Zi} {Xi+1,Zi} {Xi+1,Zi+1}.
        var indices = new ushort[(GridResolution - 1) * (GridResolution - 1) * 6];
        var idx = 0;
        for (var zIndex = 0; zIndex < GridResolution - 1; zIndex++)
        {
            for (var xIndex = 0; xIndex < GridResolution - 1; xIndex++)
            {
                var v00 = (ushort)(zIndex * GridResolution + xIndex);
                var v01 = (ushort)((zIndex + 1) * GridResolution + xIndex);
                var v10 = (ushort)(zIndex * GridResolution + xIndex + 1);
                var v11 = (ushort)((zIndex + 1) * GridResolution + xIndex + 1);

                indices[idx++] = v00;
                indices[idx++] = v01;
                indices[idx++] = v11;

                indices[idx++] = v00;
                indices[idx++] = v11;
                indices[idx++] = v10;
            }
        }

        _indices = new IndexBuffer(device, IndexElementSize.SixteenBits, indices.Length, BufferUsage.WriteOnly);
        _indices.SetData(indices);

        if (ExportHeightmapDebugImage)
            SaveHeightmapDebugImage(device);
    }

    private void SaveHeightmapDebugImage(GraphicsDevice device)
    {
        var min = float.MaxValue;
        var max = float.MinValue;
        foreach (var height in _heights)
        {
            if (height < min) min = height;
            if (height > max) max = height;
        }

        var range = max - min;
        var pixels = new Color[GridResolution * GridResolution];

        for (var zIndex = 0; zIndex < GridResolution; zIndex++)
        {
            for (var xIndex = 0; xIndex < GridResolution; xIndex++)
            {
                var normalized = range > 0f ? (_heights[xIndex, zIndex] - min) / range : 0f;
                var gray = (byte)(normalized * 255f);
                // Fila 0 de la imagen = Z más alto, para que quede orientada como
                // se ve el mapa desde arriba (no es más que una convención de
                // visualización, no afecta la generación real).
                var pixelRow = GridResolution - 1 - zIndex;
                pixels[pixelRow * GridResolution + xIndex] = new Color(gray, gray, gray);
            }
        }

        var texture = new Texture2D(device, GridResolution, GridResolution);
        texture.SetData(pixels);

        using (var stream = File.Create("heightmap_debug.png"))
        {
            texture.SaveAsPng(stream, GridResolution, GridResolution);
        }

        texture.Dispose();
    }

    // Consulta de altura interpolada, para que Forest (y más adelante el spawn de
    // tanques) ubique objetos sobre la superficie real en vez de un plano.
    public float GetHeightAt(float worldX, float worldZ)
    {
        var half = _size / 2f;
        var step = _size / (GridResolution - 1);

        var gx = MathHelper.Clamp((worldX + half) / step, 0, GridResolution - 1);
        var gz = MathHelper.Clamp((worldZ + half) / step, 0, GridResolution - 1);

        var x0 = (int)MathF.Floor(gx);
        var z0 = (int)MathF.Floor(gz);
        var x1 = Math.Min(x0 + 1, GridResolution - 1);
        var z1 = Math.Min(z0 + 1, GridResolution - 1);

        var tx = gx - x0;
        var tz = gz - z0;

        var hx0 = MathHelper.Lerp(_heights[x0, z0], _heights[x1, z0], tx);
        var hx1 = MathHelper.Lerp(_heights[x0, z1], _heights[x1, z1], tx);

        return MathHelper.Lerp(hx0, hx1, tz);
    }

    public void Draw(GraphicsDevice device, Matrix view, Matrix projection)
    {
        device.SetVertexBuffer(_vertices);
        device.Indices = _indices;

        _effect.Parameters["View"].SetValue(view);
        _effect.Parameters["Projection"].SetValue(projection);
        _effect.Parameters["World"].SetValue(_world);
        _effect.Parameters["DiffuseColor"].SetValue(_color.ToVector3());

        foreach (var pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();

            device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _indices.IndexCount / 3);
        }
    }
}
