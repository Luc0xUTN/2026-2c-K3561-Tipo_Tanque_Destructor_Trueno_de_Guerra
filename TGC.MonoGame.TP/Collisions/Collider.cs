using System;
using Microsoft.Xna.Framework;

namespace TGC.MonoGame.TP.Collisions;

public abstract class Collider
{
    protected readonly ICollidable _owner;
    protected float _boundingRadius;

    protected Collider(Entity entity)
    {
        _owner = entity;
    }

    public Matrix World => _owner.GetWorld();
    public Vector3 Position => _owner.GetPosition();
    public bool IsSolid => _owner.IsSolid;
    public bool IsStatic => _owner.IsStatic;
    public float BoundingRadius => _boundingRadius;

    public void SetPosition(Vector3 position)
    {
        _owner.SetPosition(position);
    }
}