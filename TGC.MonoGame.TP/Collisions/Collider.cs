using Microsoft.Xna.Framework;

namespace TGC.MonoGame.TP.Collisions;

public abstract class Collider
{
    protected readonly Entity _entity;

    protected Collider(Entity entity)
    {
        _entity = entity;
    }

    public Matrix World => _entity.GetWorld();
    public Vector3 Position => _entity.GetPosition();
}