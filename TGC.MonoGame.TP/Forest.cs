using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.Cameras;
using TGC.MonoGame.TP.Collisions;

namespace TGC.MonoGame.TP;

// Responsabilidad: Generar de forma procedural las árboles, rocas y otras cosas que se encuentran en el mapa
public class Forest
{
    private List<Prop> _props;
    private List<Prop> _trees;
    private List<Rock> _rocks;

    private Effect _rockEffect; 
    private Effect _treeEffect;
    
    private float _rockProbability = 0.1f;

    // Margen extra (sobre LaneHalfWidth) para que un prop no invada la lane ni
    // aunque le toque la escala más grande posible -- cubre su propio radio
    // horizontal en el peor caso + el semiancho del tanque (2u, BoxColliderFactory
    // en Tank.cs) + una holgura chica por la imprecisión del bang-bang en los
    // quiebres de la curva. Distintos porque los tamaños reales lo son:
    // - Árbol: halfWidth=0.5 (Tree.cs) con escala XZ de hasta 2x -> radio
    //   horizontal máximo ~1.4u. 1.4 + 2 + holgura ~5.
    // - Roca: la más grande de las 4 (Rock1, ver RockBounds en Rock.cs) mide
    //   ~4.2u de radio horizontal en espacio local, con escala de hasta 2x ->
    //   hasta ~8.3u real. 8.3 + 2 + holgura ~12.
    private const float TreeClearanceMargin = 5f;
    private const float RockClearanceMargin = 12f;

    private int _seed; // guardada para reutilizarla en LoadContent (selección de modelo de roca)

    public void Initialize(
        Vector2 mapSize,
        int seed,
        Func<float, float, float> getHeightAt,
        Func<float, float, float, bool> isNavigable,
        float spacing = 5f
        )
    {
        _seed = seed;
        _props = new List<Prop>();
        _trees = new List<Prop>();
        _rocks = new List<Rock>();
        var random = new Random(seed);

        int countX = (int)MathF.Floor(mapSize.X / spacing);
        int countZ = (int)MathF.Floor(mapSize.Y / spacing);

        float gridWidth = countX * spacing;
        float gridLength = countZ * spacing;

        float startX = -gridWidth / 2f + spacing * 0.5f;
        float startZ = -gridLength / 2f + spacing * 0.5f;
        float maxPositionVariation = spacing * 0.35f;

        for (int x = 0; x < countX; x++)
        {
            for (int z = 0; z < countZ; z++)
            {
                float posX = startX + (x * spacing);
                float posZ = startZ + (z * spacing);

                posX += ((float)random.NextDouble() * 2f - 1f) * maxPositionVariation;
                posZ += ((float)random.NextDouble() * 2f - 1f) * maxPositionVariation;

                // Roca o árbol se decide ACÁ (antes de chequear navegabilidad, no
                // después) porque el margen de despeje depende de cuál de los dos
                // va a terminar yendo en este punto -- una roca necesita mucho más
                // margen que un árbol (ver TreeClearanceMargin/RockClearanceMargin).
                bool isRock = random.NextDouble() < _rockProbability;
                float clearanceMargin = isRock ? RockClearanceMargin : TreeClearanceMargin;

                // Complemento de los carriles: si el punto cae sobre un carril o un
                // claro, no se pone ningún prop ahí (pipeline A->B->C, ver Battlefield).
                if (isNavigable(posX, posZ, clearanceMargin))
                    continue;

                float rColor = (float)random.NextDouble();

                if (isRock)
                {
                    Color color = Color.Lerp(Color.DimGray, Color.DarkGray, rColor);
                    float scaleVariation = (float)random.NextDouble() * 2;
                    Vector3 scale = new Vector3(scaleVariation, scaleVariation, scaleVariation);
                    int rockType = (int)random.NextInt64(4);
                    
                    var rock = new Rock(new Vector3(posX, getHeightAt(posX, posZ), posZ), scale, new Vector3(0,0,0), color, 0.2f, 0.7f, 0.2f, 16.0f, rockType);
                    rock.Initialize();

                    _rocks.Add(rock);
                    _props.Add(rock);
                }
                else
                {
                    // Cálculo para que haya una altura mínima de los árboles
                    float scaleVariationY = 0.8f + (float)random.NextDouble() * 1.5f;
                    float scaleVariationXZ = (float)random.NextDouble() + 1;
                    Vector3 scale = new Vector3(scaleVariationXZ, scaleVariationY, scaleVariationXZ);

                    var tree = new Tree(new Vector3(posX, getHeightAt(posX, posZ), posZ), scale, new Vector3(0,0,0), 0.3f, 0.8f, 0.05f, 2.0f);
                    tree.Initialize();

                    _trees.Add(tree);
                    _props.Add(tree);
                }
            }
        }
    }

    public void AddEntitiesToPhyisicSystem(PhysicsSystem physicsSystem)
    {
        _trees.ForEach(t => physicsSystem.Add(t));
        _rocks.ForEach(r => physicsSystem.Add(r));
    }

    // Destructibles: saca el prop de la lista que dibuja Draw(). No anima nada
    // todavía -- desaparece directo, queda pendiente formalizar cómo se anima
    // esa desaparición. Quitarlo de PhysicsSystem es responsabilidad de quien
    // llama a esto (GameScene), porque Forest no tiene referencia al PhysicsSystem.
    public void Remove(ICollidable collidable)
    {
        if (collidable is Prop prop)
        {
            _props.Remove(prop);
        }
    }

    public void LoadContent(Microsoft.Xna.Framework.Content.ContentManager content, string contentFolder3D, string shaderRoute)
    {
        var treeModel = content.Load<Model>(contentFolder3D + "forest/Tree/Tree");
        Texture leavesTexture = content.Load<Texture>(contentFolder3D + "forest/Tree/Tree.fbm/DB2X2_L01");
        Texture trunkTexture = content.Load<Texture>(contentFolder3D + "forest/Tree/Tree.fbm/bark_0021");

        List<Texture> treeTextures = new List<Texture>();
        treeTextures.Add(trunkTexture);
        treeTextures.Add(leavesTexture);
        
        _treeEffect  = content.Load<Effect>(shaderRoute);
        _rockEffect = content.Load<Effect>(ContentPaths.ContentFolderEffects + "BasicShader"); 
        List<Model> rockModels = new List<Model>();
        for (int i = 0; i < 4; i++)
        {
            var rockModel = content.Load<Model>($"{contentFolder3D}forest/Rocks/Rock{i}");
            rockModels.Add(rockModel);
        }
        
        _trees.ForEach(tree => tree.LoadContent(treeModel, _treeEffect, treeTextures));
        
        _rocks.ForEach(rock =>
        {
            rock.LoadContent(rockModels[rock.RockType], _rockEffect);
        });

    }

    public void Draw(
        GraphicsDevice device,
        BaseCamera camera,
        Light light,
        Matrix projection,
        BoundingFrustum boundingFrustum,
        Matrix? lightViewProjection = null,
        Texture2D shadowMap = null)
    {
        _props.ForEach(prop =>
        {
            if (boundingFrustum == null || boundingFrustum.Intersects(prop.BoundingSphere))
            {
                prop.Draw(device, camera, projection, light, lightViewProjection, shadowMap);
            }
        });
    }

    public void DrawShadowPass(
        GraphicsDevice device,
        Matrix lightView,
        Matrix lightProjection,
        Effect shadowEffect)
    {
        _props.ForEach(prop =>
        {
            prop.DrawShadowPass(device, lightView, lightProjection, shadowEffect);
        });
    }
}