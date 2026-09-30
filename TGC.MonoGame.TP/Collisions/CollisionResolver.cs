using Microsoft.Xna.Framework;

namespace TGC.MonoGame.TP.Collisions;

public static class CollisionResolver
{
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