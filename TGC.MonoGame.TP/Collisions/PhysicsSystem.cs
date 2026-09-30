using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;

namespace TGC.MonoGame.TP.Collisions;

public class PhysicsSystem
{
    private readonly List<ICollidable> _collidables = [];
    private readonly HashSet<ICollidable> _colliding = [];

    public virtual void Add(ICollidable collidable)
    {
        if (!_collidables.Contains(collidable))
        {
            _collidables.Add(collidable);
        }
    }

    public virtual void Remove(ICollidable collidable)
    {
        _collidables.Remove(collidable);
    }

    public void Update()
    {
        for (int i = 0; i < _collidables.Count; i++)
        {
            for (int j = i + 1; j < _collidables.Count; j++)
            {
                ICollidable a = _collidables[i];
                ICollidable b = _collidables[j];

                if (a.IsStatic && b.IsStatic)
                    continue;

                if (!BroadPhase(a, b))
                    continue;

                CollisionResult result =
                    CollisionDetector.CheckCollision(a, b);

                CollisionResolver.Resolve(a, b, result);

                // if (a is Tree)
                // {
                //     a.SetPosition(a.GetPosition() + new Vector3(0, 2f, 0));
                // }
                // if (b is Tree)
                // {
                //     b.SetPosition(a.GetPosition() + new Vector3(0, 2f, 0));
                // }
            }
        }
    }

    private bool BroadPhase(ICollidable a, ICollidable b)
    {
        Vector3 delta = b.GetPosition() - a.GetPosition();

        float distanceSquared = delta.LengthSquared();

        float radiusSum =
            a.Collider.BoundingRadius +
            b.Collider.BoundingRadius;

        return distanceSquared <= radiusSum * radiusSum;
    }
}