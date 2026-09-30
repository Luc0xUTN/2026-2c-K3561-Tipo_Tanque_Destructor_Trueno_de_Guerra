using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace TGC.MonoGame.TP.Collisions;

public class Face
{
    // Los índices deben estar ordenados según las agujas del reloj para que la normal se calcule bien
    public IReadOnlyList<int> VertexIndices { get; }
    public IReadOnlyList<Vector3> Vertices { get; }

    public Face(IReadOnlyList<Vector3> vertices, List<int> vertexIndices)
    {
        Vertices = vertices;
        VertexIndices = vertexIndices;
    }

    public Vector3 Normal
    {
        get // Calcula usando solo 3 puntos porque el resto deberían ser coplanares
        {
            Vector3 a = Vertices[VertexIndices[0]];
            Vector3 b = Vertices[VertexIndices[1]];
            Vector3 c = Vertices[VertexIndices[2]];

            return Vector3.Normalize(
                Vector3.Cross(b - a, c - a)
            );
        }
    }

    public Vector3 Center
    {
        get
        {
            Vector3 center = Vector3.Zero;

            foreach (var index in VertexIndices)
            {
                center += Vertices[index];
            }

            return center / VertexIndices.Count;
        }
    }
}