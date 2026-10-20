using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.Collisions;

namespace TGC.MonoGame.TP;

// Responsabilidad: proyectil disparado por un tanque. Trayectoria parabólica
// (gravedad simple sobre la componente Y de la velocidad). La colisión se
// chequea contra el SEGMENTO que recorrió en el frame (posición anterior ->
// actual), no contra un punto -- un proyectil rápido puede "saltear" un tanque
// entero entre dos posiciones discretas si solo se pregunta "¿estoy
// superpuesto ahora?" (el mismo problema, en otra escala, que hace que los
// tanques se deslicen entre colisionables -- issue #11).
public class Projectile : Entity
{
    private const float Gravity = 9.8f;
    private const float Radius = 0.3f; // chico: solo para el margen del broad-phase del segmento.
    private const float MaxLifetimeSeconds = 6f; // red de seguridad si nunca choca con nada (se fue del mapa, quedó en una parábola infinita, etc).

    // Destructibles: al impactar un prop, no solo desaparece el que registró el
    // hit -- todo lo que esté a este radio del punto de impacto también (decisión
    // de Dante: "un radio chico... no uno a uno").
    private const float DestructionRadius = 4f;

    private readonly Tank _owner;
    private Vector3 _velocity;
    private float _lifetime;

    public bool IsAlive { get; private set; } = true;

    // Props que este impacto destruyó (vacía si no hubo impacto contra el
    // escenario este frame) -- GameScene los lee después de Update() y es quien
    // efectivamente los saca de Forest/PhysicsSystem (Projectile no tiene
    // referencia a ninguno de los dos, solo "reporta" qué pasó).
    public List<ICollidable> DestroyedProps { get; } = new();

    public Projectile(Vector3 position, Vector3 velocity, Tank owner)
        : base(position, Vector3.One, Vector3.Zero, Color.Gold)
    {
        _velocity = velocity;
        _owner = owner;

        UpdateRotation();
        SetWorldMatrix();
    }

    // tanks: todos los tanques vivos (jugador + enemigos) -- se filtra acá adentro
    // quién puede recibir el impacto. staticCollidables: el escenario (árboles,
    // rocas) vía PhysicsSystem.StaticCollidables. getHeightAt: altura real del
    // terreno (Battlefield.GetHeightAt) -- regla general del proyectil: cualquier
    // colisión lo destruye, el piso no es la excepción.
    public void Update(float elapsedTime, IReadOnlyList<Tank> tanks, IReadOnlyList<ICollidable> staticCollidables, Func<float, float, float> getHeightAt)
    {
        if (!IsAlive) return;

        _lifetime += elapsedTime;
        if (_lifetime >= MaxLifetimeSeconds)
        {
            IsAlive = false;
            return;
        }

        _velocity.Y -= Gravity * elapsedTime;

        var previousPosition = _position;
        var nextPosition = _position + _velocity * elapsedTime;

        var groundHeight = getHeightAt(nextPosition.X, nextPosition.Z);
        if (nextPosition.Y <= groundHeight)
        {
            var groundPoint = new Vector3(nextPosition.X, groundHeight, nextPosition.Z);
            IsAlive = false;
            Console.WriteLine($"Projectile impact on ground at {groundPoint}");
            return;
        }

        foreach (var tank in tanks)
        {
            if (tank == _owner) continue; // el spawn ya lo evita, esto es una red de seguridad barata.
            if (!GameSettings.FriendlyFireEnabled && SameTeam(_owner, tank)) continue;

            if (TryHit(tank.Collider, previousPosition, nextPosition, out var hitPoint))
            {
                IsAlive = false;
                tank.TakeProjectileDamage();
                Console.WriteLine($"Projectile impact on {tank.GetType().Name} at {hitPoint} -- vida restante: {tank.Health}");
                return;
            }
        }

        foreach (var collidable in staticCollidables)
        {
            if (TryHit(collidable.Collider, previousPosition, nextPosition, out var hitPoint))
            {
                IsAlive = false;
                DestroyNearbyProps(hitPoint, staticCollidables);
                Console.WriteLine($"Projectile impact on prop at {hitPoint} -- {DestroyedProps.Count} prop(s) destruidos");
                return;
            }
        }

        _position = nextPosition;
        SetWorldMatrix();
    }

    private bool TryHit(Collider collider, Vector3 from, Vector3 to, out Vector3 hitPoint)
    {
        hitPoint = Vector3.Zero;

        // Broad-phase: distancia del CENTRO del collider al SEGMENTO completo
        // (no a la posición actual) -- así no importa qué tan rápido viaje el
        // proyectil entre un frame y el siguiente, el chequeo mira todo el camino.
        var closest = ClosestPointOnSegment(collider.Position, from, to);
        var maxDistance = collider.BoundingRadius + Radius;
        if (Vector3.DistanceSquared(collider.Position, closest) > maxDistance * maxDistance)
            return false;

        if (collider is ConvexCollider convexCollider &&
            convexCollider.TryIntersectSegment(from, to, out var t, out _))
        {
            hitPoint = from + (to - from) * t;
            return true;
        }

        return false;
    }

    private static Vector3 ClosestPointOnSegment(Vector3 point, Vector3 a, Vector3 b)
    {
        var ab = b - a;
        var lengthSquared = ab.LengthSquared();
        if (lengthSquared < 1e-6f) return a;

        var t = MathHelper.Clamp(Vector3.Dot(point - a, ab) / lengthSquared, 0f, 1f);
        return a + ab * t;
    }

    // Único concepto de "equipo" que existe hoy: jugador vs. cualquier enemigo.
    // Si más adelante hay más de un jugador o facciones, esto tiene que cambiar.
    private static bool SameTeam(Tank a, Tank b)
    {
        return (a is PlayerTank) == (b is PlayerTank);
    }

    // Todo lo estático que esté a DestructionRadius del punto de impacto se
    // destruye, no solo el objeto puntual contra el que el segmento dio el hit.
    private void DestroyNearbyProps(Vector3 hitPoint, IReadOnlyList<ICollidable> staticCollidables)
    {
        var radiusSquared = DestructionRadius * DestructionRadius;
        foreach (var collidable in staticCollidables)
        {
            if (Vector3.DistanceSquared(collidable.GetPosition(), hitPoint) <= radiusSquared)
            {
                DestroyedProps.Add(collidable);
            }
        }
    }

    protected override void DrawMeshes()
    {
        foreach (var mesh in _model.Meshes)
        {
            _effect.Parameters["World"].SetValue(mesh.ParentBone.Transform * _world);
            mesh.Draw();
        }
    }

    private void UpdateRotation()
    {
        if (_velocity.LengthSquared() < 1e-6f)
            return;

        Vector3 direction = Vector3.Normalize(_velocity);

        _rotation = Quaternion.CreateFromRotationMatrix(
            Matrix.CreateWorld(Vector3.Zero, direction, Vector3.Up)
        );
    }
}
