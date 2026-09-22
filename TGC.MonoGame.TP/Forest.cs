using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP;

// Responsabilidad: Generar de forma procedural las árboles, rocas y otras cosas que se encuentran en el mapa
public class Forest
{
    private List<Prop> _props;
    private List<Prop> _trees;
    private List<Prop> _rocks;

    private float _rockProbability = 0.1f;


    public void Initialize(Vector2 mapSize, float spacing = 5f)
    {
        _props = new List<Prop>();
        _trees = new List<Prop>();
        _rocks = new List<Prop>();
        var random = new Random();

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

                float rColor = (float)random.NextDouble();

                if (random.NextDouble() < _rockProbability)
                {
                    Color color = Color.Lerp(Color.DimGray, Color.DarkGray, rColor);
                    float scaleVariation = (float)random.NextDouble() * 2;
                    Vector3 scale = new Vector3(scaleVariation, scaleVariation, scaleVariation);

                    var rock = new Prop(new Vector3(posX, 0, posZ), scale, new Vector3(0,0,0), color);
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

                    var tree = new Prop(new Vector3(posX, 0, posZ), scale, new Vector3(0,0,0), color);
                    tree.Initialize();

                    _trees.Add(tree);
                    _props.Add(tree);    
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

        var random = new Random();
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