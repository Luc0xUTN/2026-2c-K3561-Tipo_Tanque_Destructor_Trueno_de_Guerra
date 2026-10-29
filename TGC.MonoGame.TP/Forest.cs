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
    private List<Rock> _rocks;

    private float _rockProbability = 0.1f;

    private int _seed; // guardada para reutilizarla en LoadContent (selección de modelo de roca)

    public void Initialize(
        Vector2 mapSize,
        int seed,
        Func<float, float, float> getHeightAt,
        Func<float, float, bool> isNavigable,
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

                    var rock = new Rock(new Vector3(posX, getHeightAt(posX, posZ), posZ), scale, new Vector3(0,0,0), color);
                    rock.Initialize();

                    _rocks.Add(rock);
                    _props.Add(rock);
                }
                else
                {
                    float scaleVariationY = (float)random.NextDouble() * 2;
                    float scaleVariationXZ = (float)random.NextDouble() + 1;
                    Vector3 scale = new Vector3(scaleVariationXZ, scaleVariationY, scaleVariationXZ);

                    var tree = new Tree(new Vector3(posX, getHeightAt(posX, posZ), posZ), scale, new Vector3(0,0,0));
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
        
        var treeEffect = content.Load<Effect>(shaderRoute);
        var rockEffect = content.Load<Effect>(ContentPaths.ContentFolderEffects + "BasicShader"); 
        List<Model> rockModels = new List<Model>();
        for (int i = 0; i < 4; i++)
        {
            var rockModel = content.Load<Model>($"{contentFolder3D}forest/Rocks/Rock{i}");
            rockModels.Add(rockModel);
        }
        
        _trees.ForEach(tree => tree.LoadContent(treeModel, treeEffect, treeTextures));
        
        // Misma seed que Initialize, en una instancia de Random independiente: esto
        // solo elige variedad visual (qué modelo de roca usar), no posiciones.
        var random = new Random(_seed);
        _rocks.ForEach(rock =>
        {
            int rockType = (int)random.NextInt64(rockModels.Count);
            rock.LoadContent(rockModels[rockType], rockEffect);
        });

    }

    public void Draw(GraphicsDevice device, Matrix view, Matrix projection, BoundingFrustum boundingFrustum)
    {
        _props.ForEach(prop =>
        {
            if (boundingFrustum.Intersects(prop.BoundingSphere))
                prop.Draw(device, view, projection); 
        });
    }
}