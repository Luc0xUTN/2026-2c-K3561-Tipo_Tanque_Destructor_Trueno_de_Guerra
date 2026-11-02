using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using TGC.MonoGame.TP;

namespace TGC.MonoGame.TP.Collisions;

public class PhysicsSystem
{
    // Separados desde el Add(): dos estáticos nunca pueden empezar a
    // colisionar entre sí (ninguno se mueve), así que ese par no se
    // vuelve a generar en Update() -- antes se enumeraba y se
    // descartaba con "a.IsStatic && b.IsStatic", que igual cuesta
    // recorrer con miles de estáticos.
    private readonly List<ICollidable> _staticCollidables = [];
    private readonly List<ICollidable> _dynamicCollidables = [];

    // Pares que están tocándose ESTE Update (se arma de cero cada vez) vs. el
    // Update anterior -- así se puede distinguir "arranca un contacto nuevo"
    // de "sigue tocándose". El daño por choque (ver CheckPair) solo se aplica
    // en el primer caso: con un tanque trabado contra un obstáculo
    // insistiendo en avanzar (issue #11), el par queda tocándose
    // indefinidamente -- sin este chequeo, seguiría perdiendo vida cada
    // CollisionDamageCooldownSeconds para siempre, aunque el clipping de
    // velocidad (ver ApplyVelocityClip) ya evite que la física en sí se vea
    // mal. Doble buffer (swap al final de Update) para no reasignar listas
    // nuevas cada frame.
    private HashSet<(ICollidable, ICollidable)> _collidingPairs = new();
    private HashSet<(ICollidable, ICollidable)> _previousCollidingPairs = new();

    // Props destruidos por choque de tanque en el Update() más reciente (se
    // vacía solo al empezar cada Update -- ver abajo). Mismo circuito que
    // Projectile.DestroyedProps: PhysicsSystem solo "reporta" qué pasó, es
    // GameScene quien efectivamente los saca de Forest y de acá.
    private readonly List<ICollidable> _destroyedProps = [];
    public IReadOnlyList<ICollidable> DestroyedProps => _destroyedProps;

    // Lectura para Projectile: necesita testear su segmento de movimiento contra
    // el escenario (árboles, rocas), no solo contra los tanques que ya conoce
    // GameScene directo.
    public IReadOnlyList<ICollidable> StaticCollidables => _staticCollidables;

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
        _destroyedProps.Clear();
        _collidingPairs.Clear();

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

        // _collidingPairs (lo que se acaba de armar este Update) pasa a ser
        // "el anterior" para el próximo -- el que hoy era "el anterior" queda
        // reciclado como el próximo buffer (se vacía al principio del próximo
        // Update, arriba).
        (_previousCollidingPairs, _collidingPairs) = (_collidingPairs, _previousCollidingPairs);
    }

    private void CheckPair(ICollidable a, ICollidable b)
    {
        if (!BroadPhase(a, b))
            return;

        CollisionResult result = CollisionDetector.CheckCollision(a, b);

        if (!result.IsColliding) return;

        _collidingPairs.Add((a, b));
        bool isNewContact = !_previousCollidingPairs.Contains((a, b));

        // Prop destructible (Tree): se resuelve DISTINTO a un choque normal --
        // sin corrección de posición (el tanque sigue avanzando hacia donde
        // estaba el prop, no rebota), con un frenado parcial en vez de un
        // clipping a cero, y queda reportado para que GameScene lo saque de
        // escena. No llama a CollisionResolver.Resolve para este par -- ver
        // Prop.DestroyedOnTankCollision/Tree.cs para el detalle del diseño.
        if (TryHandleDestructibleProp(a, b, isNewContact)) return;
        if (TryHandleDestructibleProp(b, a, isNewContact)) return;

        // Choque "normal" (sólido: roca, otro tanque) -- corrige posición
        // (como siempre) Y frena, de la velocidad de cada lado dinámico, SOLO
        // la componente que entra al otro (clipping, ver CollisionResolver).
        // Sin esto el tanque corregía de posición cada frame pero la
        // velocidad acumulada seguía empujando hacia el obstáculo -- quedaba
        // trabado en el límite de contacto perdiendo vida para siempre
        // (issue #11, confirmado con una simulación aparte antes de tocar
        // esto: 15s sin moverse un milímetro, -2 de vida cada 0.5s, sin
        // recuperarse nunca).
        CollisionResolver.Resolve(a, b, result);
        ApplyVelocityClip(a, -result.Normal);
        ApplyVelocityClip(b, result.Normal);

        // Daño por choque físico -- a propósito NO vive dentro de CollisionResolver
        // (esa clase resuelve física, no debería saber nada de "vida"). Se
        // aplica acá, como un paso aparte, y SOLO si el contacto es nuevo --
        // el clipping de velocidad por sí solo no alcanzaba para que un
        // tanque insistiendo contra un obstáculo deje de perder vida (otra
        // simulación aparte lo confirmó: seguía bajando cada 0.5s,
        // indefinidamente, aunque ya no se moviera). El cooldown de
        // Tank.TakeCollisionDamage se mantiene además de esto, como red de
        // seguridad para contactos nuevos simultáneos.
        if (isNewContact)
        {
            ApplyCollisionDamage(a, Tank.DefaultCollisionDamage);
            ApplyCollisionDamage(b, Tank.DefaultCollisionDamage);
        }
    }

    // outwardNormal: apunta ALEJÁNDOSE del otro objeto del par, hacia donde
    // está collidable -- result.Normal ya apunta de a hacia b, así que para
    // b se usa tal cual y para a invertido (ver el llamador).
    private static void ApplyVelocityClip(ICollidable collidable, Vector3 outwardNormal)
    {
        if (collidable is not Tank tank) return; // solo Tank tiene velocidad propia hoy -- los props son Vector3.Zero, no hay nada que frenarles.

        var velocity = tank.GetVelocity();
        var forward = tank.GetWorld().Forward;
        var originalForwardSpeed = Vector3.Dot(velocity, forward);
        if (MathF.Abs(originalForwardSpeed) < 1e-4f) return; // no se estaba moviendo -- nada que frenar, evita dividir por ~0 abajo.

        var clippedVelocity = CollisionResolver.ClipVelocity(velocity, outwardNormal);
        var clippedForwardSpeed = Vector3.Dot(clippedVelocity, forward);

        // Clamp [0,1]: nunca lo acelera (el clipping solo puede quitar
        // energía) ni lo manda en reversa (un choque no debería invertirle la
        // marcha solo).
        var scale = Math.Clamp(clippedForwardSpeed / originalForwardSpeed, 0f, 1f);
        tank.ApplyCollisionSpeedScale(scale);
    }

    private bool TryHandleDestructibleProp(ICollidable maybeTank, ICollidable maybeProp, bool isNewContact)
    {
        if (maybeTank is not Tank tank) return false;
        if (maybeProp is not Prop prop || !prop.DestroyedOnTankCollision) return false;

        // El frenado y la destrucción se reportan SIEMPRE que haya contacto
        // (es física/estado, no "vida") -- el daño, en cambio, solo en el
        // contacto nuevo, misma regla que el resto de CheckPair. En la
        // práctica casi no se nota la diferencia acá (el prop desaparece casi
        // al toque), pero mantiene una sola regla en todo el archivo.
        tank.ApplyCollisionSpeedScale(prop.TankCollisionSpeedMultiplier);
        if (isNewContact)
        {
            tank.TakeCollisionDamage(prop.TankCollisionDamage);
        }
        _destroyedProps.Add(maybeProp);

        return true;
    }

    private static void ApplyCollisionDamage(ICollidable collidable, int amount)
    {
        if (collidable is Tank tank)
        {
            tank.TakeCollisionDamage(amount);
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