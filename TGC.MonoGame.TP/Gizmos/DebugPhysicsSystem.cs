using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.Collisions;

namespace TGC.MonoGame.TP;

public class DebugPhysicsSystem : PhysicsSystem
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly Dictionary<ICollidable, ColliderGizmo> _gizmos = [];

    public DebugPhysicsSystem(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public override void Add(ICollidable collidable)
    {
        base.Add(collidable);

        if (collidable.Collider is ConvexCollider convexCollider)
        {
            _gizmos.Add(
                collidable,
                new ColliderGizmo(
                    _graphicsDevice,
                    convexCollider
                )
            );
        }
    }

    public override void Remove(ICollidable collidable)
    {
        base.Remove(collidable);
        _gizmos.Remove(collidable);
    }

    public void Draw(Matrix view, Matrix projection)
    {
        foreach (var gizmo in _gizmos.Values)
        {
            gizmo.DrawDebug(
                _graphicsDevice,
                view,
                projection
            );
        }
    }
}