using Microsoft.Xna.Framework;
using TGC.MonoGame.TP.Collisions;

public interface ICollidable
{
    bool IsStatic { get; }
    bool IsSolid { get; }

    Vector3 GetPosition();
    Vector3 GetScale();
    Vector3 GetRotation();
    Matrix GetWorld();

    Collider Collider { get; }

    void SetPosition(Vector3 position);
}