using Microsoft.Xna.Framework;
using TGC.MonoGame.TP.Collisions;

public interface ICollidable
{
    bool IsStatic { get; }
    bool IsSolid { get; }

    Vector3 GetPosition();
    Vector3 GetScale();
    Quaternion GetRotation();
    Matrix GetWorld();

    // Velocidad lineal actual en espacio de mundo -- Vector3.Zero para
    // cualquier cosa que no se mueva por sus propios medios (ver Entity).
    // La usa CollisionResolver para el clipping de velocidad en el choque
    // (cancelar la componente que entra al otro objeto, sin tocar la que es
    // paralela a la superficie).
    Vector3 GetVelocity();

    Collider Collider { get; }

    void SetPosition(Vector3 position);
}