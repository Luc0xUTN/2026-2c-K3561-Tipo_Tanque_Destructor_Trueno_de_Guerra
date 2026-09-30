using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace TGC.MonoGame.TP.Collisions;

public static class CollisionDetector
{
    public static CollisionResult CheckCollision(ICollidable a, ICollidable b)
    {
        if (a.Collider is ConvexCollider convexA && b.Collider is ConvexCollider convexB)
        {
            return CheckConvexConvex(convexA, convexB);
        }

        return new CollisionResult(
            false,
            Vector3.Zero,
            0
        );
    }

    private static CollisionResult CheckConvexConvex(ConvexCollider a, ConvexCollider b)
    {
        var axes = GetSATCandidateAxes(a, b);

        float minimumPenetration = float.MaxValue;
        Vector3 minimumAxis = Vector3.Zero;

        foreach (var axis in axes)
        {
            var projectionA = Project(a, axis);
            var projectionB = Project(b, axis);

            float penetration =
                MathF.Min(projectionA.Max, projectionB.Max) - MathF.Max(projectionA.Min, projectionB.Min);

            if (penetration < 0) // No intersectan en este eje
                return new CollisionResult(false, Vector3.Zero, 0);
            
            if (penetration < minimumPenetration)
            {
                minimumPenetration = penetration;
                minimumAxis = axis;
            }
        }

        Vector3 direction = b.Position - a.Position; // Revisa que el eje mínimo esté orientado de A a B.
        if (Vector3.Dot(direction, minimumAxis) < 0)
        {
            minimumAxis = -minimumAxis;
        }

        return new CollisionResult(true, minimumAxis, minimumPenetration);
    }

    // Proyecta un ConvexCollider sobre un eje
    private static (float Min, float Max) Project(ConvexCollider collider, Vector3 axis)
    {
        float min = float.MaxValue;
        float max = float.MinValue;

        foreach (var vertex in collider.GetWorldVertices())
        {
            float projection = Vector3.Dot(vertex, axis);

            if (projection < min) min = projection;

            if (projection > max) max = projection;
        }

        return (min, max);
    }

    private static List<Vector3> GetSATCandidateAxes(ConvexCollider a, ConvexCollider b)
    {
        var axes = new List<Vector3>();

        foreach (var face in a.Faces) // Normales de las caras de A
        {
            Vector3 normal = Vector3.TransformNormal(face.Normal, a.World);

            normal.Normalize();
            axes.Add(normal);
        }

        foreach (var face in b.Faces) // Normales de las caras de B
        {
            Vector3 normal = Vector3.TransformNormal(face.Normal, b.World);

            normal.Normalize();
            axes.Add(normal);
        }

        foreach (var edgeA in a.GetEdges()) // Productos vectoriales entre las aristas de A y B
        {
            Vector3 directionA = GetWorldEdgeDirection(a, edgeA);

            foreach (var edgeB in b.GetEdges())
            {
                Vector3 directionB = GetWorldEdgeDirection(b, edgeB);
                Vector3 axis = Vector3.Cross(directionA, directionB);

                if (axis.LengthSquared() > 0)
                {
                    axis.Normalize();
                    axes.Add(axis);
                }
            }
        }

        return axes;
    }


    private static Vector3 GetWorldEdgeDirection(ConvexCollider collider, Edge edge)
    {
        Vector3 direction = collider.Vertices[edge.B] - collider.Vertices[edge.A];
        return Vector3.TransformNormal(direction, collider.World);
    }
}