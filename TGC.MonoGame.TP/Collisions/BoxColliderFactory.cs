using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace TGC.MonoGame.TP.Collisions;

// Responsabilidad: armar los vértices/caras de una caja (en espacio local, base
// en Y=0) para un ConvexCollider. Antes esto estaba copy-pasteado a mano en
// Tank/Tree -- un collider nuevo (ej. Rock) era otra copia más.
public static class BoxColliderFactory
{
    public static (List<Vector3> vertices, List<Face> faces) CreateBox(float halfWidth, float height)
    {
        return CreateBox(halfWidth, 0f, height, halfWidth);
    }

    public static (List<Vector3> vertices, List<Face> faces) CreateBox(float halfWidth, float minY, float maxY, float halfDepth)
    {
        return CreateBoxFromBounds(
            new Vector3(-halfWidth, minY, -halfDepth),
            new Vector3( halfWidth, maxY,  halfDepth)
        );
    }

    public static (List<Vector3> vertices, List<Face> faces) CreateBoxFromBounds(Vector3 min, Vector3 max)
    {
        var vertices = new List<Vector3>
        {
            new(min.X, min.Y, min.Z),
            new(max.X, min.Y, min.Z),
            new(max.X, min.Y, max.Z),
            new(min.X, min.Y, max.Z),

            new(min.X, max.Y, min.Z),
            new(max.X, max.Y, min.Z),
            new(max.X, max.Y, max.Z),
            new(min.X, max.Y, max.Z)
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

        return (vertices, faces);
    }
}
