using Microsoft.Xna.Framework;

namespace TGC.MonoGame.TP.Collisions;

public struct CollisionResult
{
    public bool IsColliding { get; }
    public Vector3 Normal { get; }
    public float Penetration { get; }

    public CollisionResult(bool isColliding, Vector3 normal, float penetration)
    {
        IsColliding = isColliding;
        Normal = normal;
        Penetration = penetration;
    }

    public override readonly string ToString()
    {
        return $"CollisionResult {{ IsColliding: {IsColliding}, Normal: {Normal}, Penetration: {Penetration} }}";
    }
}