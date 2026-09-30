
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TGC.MonoGame.TP.Collisions;

namespace TGC.MonoGame.TP;

public class Tree : Prop
{
    public Tree(Vector3 position, Vector3 scale, Vector3 rotation, Color color) : base(position, scale, rotation, color)
    {
        IsSolid = true;
        var vertices = new List<Vector3>
        {
            new(-0.5f, 0, -0.5f),
            new( 0.5f, 0, -0.5f),
            new( 0.5f, 0,  0.5f),
            new(-0.5f, 0,  0.5f),

            new(-0.5f, 3, -0.5f),
            new( 0.5f, 3, -0.5f),
            new( 0.5f, 3,  0.5f),
            new(-0.5f, 3,  0.5f)
        };

        var faces = new List<Face>
        {
            new(vertices, new List<int> { 0, 1, 2, 3 }),
            new(vertices, new List<int> { 0, 4, 5, 1 }),
            new(vertices, new List<int> { 4, 7, 6, 5 }),
            new(vertices, new List<int> { 1, 5, 6, 2 }),
            new(vertices, new List<int> { 2, 6, 7, 3 }),
            new(vertices, new List<int> { 3, 7, 4, 0 })
        };

        Collider = new ConvexCollider(this, vertices, faces);
    }
}