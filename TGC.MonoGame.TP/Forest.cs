using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.Collisions;

namespace TGC.MonoGame.TP;

// Responsabilidad: Generar de forma procedural las árboles, rocas y otras cosas que se encuentran en el mapa
public class Forest
{
    private List<Prop> _props;
    private List<Prop> _trees;
    private List<Prop> _rocks;

    private float _rockProbability = 0.1f;

    private int _seed; // guardada para reutilizarla en LoadContent (selección de modelo de roca)

    public void Initialize(
        Vector2 mapSize,
        int seed,
        Func<float, float, float> getHeightAt,
        Func<float, float, bool> isNavigable,
        PhysicsSystem physicsSystem,
        float spacing = 5f
        )
    {
        _seed = seed;
        _props = new List<Prop>();
        _trees = new List<Prop>();
        _rocks = new List<Prop>();
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

                // Complemento de los carriles: si el punto cae sobre un carril o un
                // claro, no se pone ningún prop ahí (pipeline A->B->C, ver Battlefield).
                if (isNavigable(posX, posZ))
                    continue;

                float rColor = (float)random.NextDouble();

                if (random.NextDouble() < _rockProbability)
                {
                    Color color = Color.Lerp(Color.DimGray, Color.DarkGray, rColor);
                    float scaleVariation = (float)random.NextDouble() * 2;
                    Vector3 scale = new Vector3(scaleVariation, scaleVariation, scaleVariation);

                    var rock = new Prop(new Vector3(posX, getHeightAt(posX, posZ), posZ), scale, new Vector3(0,0,0), color);
                    rock.Initialize();

                    _rocks.Add(rock);
                    _props.Add(rock);
                }
                else
                {
                    Color color = Color.Lerp(Color.SaddleBrown, Color.DarkGreen, rColor);

                    float scaleVariationY = (float)random.NextDouble() * 2;
                    float scaleVariationXZ = (float)random.NextDouble() + 1;
                    Vector3 scale = new Vector3(scaleVariationXZ, scaleVariationY, scaleVariationXZ);

                    var tree = new Tree(new Vector3(posX, getHeightAt(posX, posZ), posZ), scale, new Vector3(0,0,0), color);
                    tree.Initialize();

                    _trees.Add(tree);
                    _props.Add(tree);
                    physicsSystem.Add(tree);
                }
            }
        }
    }

    public void LoadContent(ContentManager content, string contentFolder3D, string shaderRoute)
    {
        var treeModel = content.Load<Model>(contentFolder3D + "forest/Tree/Tree");
        var effect = content.Load<Effect>(shaderRoute);
        List<Model> rockModels = new List<Model>();
        for (int i = 0; i < 4; i++)
        {
            var rockModel = content.Load<Model>($"{contentFolder3D}forest/Rocks/Rock{i}");
            rockModels.Add(rockModel);
        }
        
        _trees.ForEach(tree => tree.LoadContent(treeModel, effect));

        // Misma seed que Initialize, en una instancia de Random independiente: esto
        // solo elige variedad visual (qué modelo de roca usar), no posiciones.
        var random = new Random(_seed);
        _rocks.ForEach(rock =>
        {
            int rockType = (int)random.NextInt64(rockModels.Count);
            rock.LoadContent(rockModels[rockType], effect);
        });

    }

    public void Draw(GraphicsDevice device, Matrix view, Matrix projection)
    {
        _props.ForEach(prop => prop.Draw(device, view, projection));
    }
}