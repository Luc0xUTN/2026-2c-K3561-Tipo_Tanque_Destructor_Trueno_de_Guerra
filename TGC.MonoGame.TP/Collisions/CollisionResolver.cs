using System;
using Microsoft.Xna.Framework;

namespace TGC.MonoGame.TP.Collisions;

public static class CollisionResolver
{
    // Clipping de velocidad: cancela, de una velocidad dada, SOLO la
    // componente que entra al obstáculo -- deja intacta la componente
    // paralela a la superficie (por eso un choque de costado frena mucho
    // menos que uno de frente; el de costado tiene dot(velocity,outwardNormal)
    // ~0, así que casi no se le resta nada). outwardNormal apunta ALEJÁNDOSE
    // del obstáculo, hacia donde está el que choca -- "entrar" es tener una
    // componente negativa en esa dirección. Fórmula estándar (la misma que
    // "slide along walls" de cualquier motor tipo Quake).
    public static Vector3 ClipVelocity(Vector3 velocity, Vector3 outwardNormal)
    {
        float inward = MathF.Min(0f, Vector3.Dot(velocity, outwardNormal));
        return velocity - outwardNormal * inward;
    }

    public static void Resolve(ICollidable a, ICollidable b, CollisionResult result)
    {
        if (!result.IsColliding) return;
        if (!a.IsSolid || !b.IsSolid) return;
        if (a.IsStatic && b.IsStatic) return;

        Vector3 correction = result.Normal * result.Penetration;

        if (a.IsStatic)
        {
            b.SetPosition(b.GetPosition() + correction);
            return;
        }
        if (b.IsStatic)
        {
            a.SetPosition(a.GetPosition() - correction);
            return;
        }
        a.SetPosition(a.GetPosition() - correction / 2);
        b.SetPosition(b.GetPosition() + correction / 2);
    }
}