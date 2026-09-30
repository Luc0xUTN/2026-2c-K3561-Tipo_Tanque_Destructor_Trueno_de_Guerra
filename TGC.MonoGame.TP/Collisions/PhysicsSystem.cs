using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;

namespace TGC.MonoGame.TP.Collisions;

public class PhysicsSystem
{
    // Separados desde el Add(): dos estáticos nunca pueden empezar a
    // colisionar entre sí (ninguno se mueve), así que ese par no se
    // vuelve a generar en Update() -- antes se enumeraba y se
    // descartaba con "a.IsStatic && b.IsStatic", que igual cuesta
    // recorrer con miles de estáticos (ver progress/10 en el repo de
    // contexto).
    private readonly List<ICollidable> _staticCollidables = [];
    private readonly List<ICollidable> _dynamicCollidables = [];
    private readonly HashSet<ICollidable> _colliding = [];

    public virtual void Add(ICollidable collidable)
    {
        var list = collidable.IsStatic ? _staticCollidables : _dynamicCollidables;
        if (!list.Contains(collidable))
        {
            list.Add(collidable);
        }
    }

    public virtual void Remove(ICollidable collidable)
    {
        var list = collidable.IsStatic ? _staticCollidables : _dynamicCollidables;
        list.Remove(collidable);
    }

    public void Update()
    {
        // Dinámico x dinámico: cada par se chequea una sola vez.
        for (int i = 0; i < _dynamicCollidables.Count; i++)
        {
            for (int j = i + 1; j < _dynamicCollidables.Count; j++)
            {
                CheckPair(_dynamicCollidables[i], _dynamicCollidables[j]);
            }
        }

        // Dinámico x estático: no hace falta estático x estático (ver
        // comentario arriba de los campos).
        for (int i = 0; i < _dynamicCollidables.Count; i++)
        {
            for (int j = 0; j < _staticCollidables.Count; j++)
            {
                CheckPair(_dynamicCollidables[i], _staticCollidables[j]);
            }
        }
    }

    private void CheckPair(ICollidable a, ICollidable b)
    {
        if (!BroadPhase(a, b))
            return;

        CollisionResult result = CollisionDetector.CheckCollision(a, b);

        CollisionResolver.Resolve(a, b, result);
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