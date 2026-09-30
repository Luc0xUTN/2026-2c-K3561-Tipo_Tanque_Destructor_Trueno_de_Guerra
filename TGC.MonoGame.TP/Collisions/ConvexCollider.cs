using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace TGC.MonoGame.TP.Collisions;

public class ConvexCollider : Collider
{
    public IReadOnlyList<Vector3> Vertices { get; }
    public IReadOnlyList<Face> Faces { get; }

    public ConvexCollider(Entity entity, List<Vector3> vertices, List<Face> faces) : base(entity)
    {
        Vertices = vertices;
        Faces = faces;

        float maxDistanceSquared = 0;
        foreach (var vertex in vertices)
        {
            maxDistanceSquared = MathF.Max(
                maxDistanceSquared,
                vertex.LengthSquared()
            );
        }

        float localRadius = MathF.Sqrt(maxDistanceSquared);

        float maxScale = MathF.Max(
            MathF.Abs(_owner.GetScale().X),
            MathF.Max(
                MathF.Abs(_owner.GetScale().Y),
                MathF.Abs(_owner.GetScale().Z)
            )
        );

        _boundingRadius = localRadius * maxScale;
    }

    public IEnumerable<Edge> GetEdges()
    {
        var edges = new HashSet<Edge>();

        foreach (var face in Faces)
        {
            for (int i = 0; i < face.VertexIndices.Count; i++)
            {
                int currentIndex = face.VertexIndices[i];
                int nextIndex = face.VertexIndices[
                    (i + 1) % face.VertexIndices.Count
                ];

                edges.Add(new Edge(currentIndex, nextIndex));
            }
        }

        return edges;
    }

    public List<Vector3> GetWorldVertices()
    {
        var result = new List<Vector3>();

        foreach (var vertex in Vertices)
        {
            result.Add(Vector3.Transform(vertex, World));
        }

        return result;
    }

    public List<Vector3> GetWorldNormals()
    {
        var result = new List<Vector3>();

        foreach (var face in Faces)
        {
            Vector3 normal = Vector3.TransformNormal(
                face.Normal,
                World
            );

            result.Add(Vector3.Normalize(normal));
        }

        return result;
    }
}