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

    // Recorta el segmento a-b contra cada plano de cara del poliedro (Cyrus-Beck
    // generalizado a 3D) -- lo usa Projectile para CCD: en vez de preguntar "¿estoy
    // superpuesto ahora?" en cada frame (lo que un proyectil rápido puede saltear
    // sin tocar nunca el collider), pregunta "¿el camino que recorrí este frame
    // cruzó el poliedro en algún punto?". tHit es el punto de entrada, en [0,1]
    // sobre el segmento; hitNormal es la normal de la cara por la que entró.
    public bool TryIntersectSegment(Vector3 a, Vector3 b, out float tHit, out Vector3 hitNormal)
    {
        var worldVertices = GetWorldVertices();
        var worldNormals = GetWorldNormals();
        var direction = b - a;

        var tMin = 0f;
        var tMax = 1f;
        var enterNormal = Vector3.Zero;

        for (var i = 0; i < Faces.Count; i++)
        {
            var normal = worldNormals[i];
            var pointOnFace = worldVertices[Faces[i].VertexIndices[0]];

            // w: qué tan "afuera" (en la dirección de la normal) arranca el segmento
            // respecto de este plano. d: qué tan rápido se mueve hacia adentro/afuera.
            var w = Vector3.Dot(a - pointOnFace, normal);
            var d = Vector3.Dot(direction, normal);

            if (MathF.Abs(d) < 1e-6f)
            {
                // Segmento paralelo a esta cara: si ya arrancó afuera, nunca va a
                // entrar por acá -- descarta todo el segmento.
                if (w > 0f)
                {
                    tHit = 0f;
                    hitNormal = Vector3.Zero;
                    return false;
                }
                continue;
            }

            var t = -w / d;
            if (d > 0f)
            {
                // Saliendo por este plano: pone un techo a t.
                if (t < tMax) tMax = t;
            }
            else
            {
                // Entrando por este plano: pone un piso a t.
                if (t > tMin)
                {
                    tMin = t;
                    enterNormal = normal;
                }
            }

            if (tMin > tMax)
            {
                tHit = 0f;
                hitNormal = Vector3.Zero;
                return false;
            }
        }

        tHit = tMin;
        hitNormal = enterNormal;
        return true;
    }
}