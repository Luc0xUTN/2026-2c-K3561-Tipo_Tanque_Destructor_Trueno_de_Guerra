using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace TGC.MonoGame.TP;

// Lo que decide AI en un tick: EnemyTank es quien la ejecuta, porque Rotate/Move
// son protected en Tank y AI no hereda de ahí.
public readonly struct TankIntention
{
    public int RotateDirection { get; }
    public int MoveDirection { get; }
    public bool ShouldFire { get; }

    public TankIntention(int rotateDirection, int moveDirection, bool shouldFire)
    {
        RotateDirection = rotateDirection;
        MoveDirection = moveDirection;
        ShouldFire = shouldFire;
    }
}

// Responsabilidad: decidir qué hace un tanque enemigo cada tick (perseguir al
// jugador sin salirse de las lanes navegables, disparar si está cerca). No toca
// Tank directo -- devuelve una intención, EnemyTank la aplica.
public class AI
{
    // Valores estimados a ojo contra la escala del mapa/tanque (dummy, sin ajustar):
    private const float AngleTolerance = 0.05f; // ~3°, por debajo se considera "ya alineado".
    private const float FireRadius = 40f; // ~3x el ancho de un carril (14u): distancia de combate, no todo el mapa.
    private const float FireCooldownSeconds = 2f;
    private const float StopRadius = 15f; // ~4x el ancho del tanque (3.69u): frena antes de superponerse al jugador.

    private readonly Entity _player;
    private readonly Battlefield _battlefield;

    private float _fireTimer;

    public AI(Entity player, Battlefield battlefield)
    {
        _player = player;
        _battlefield = battlefield;
        _fireTimer = 0f;
    }

    public TankIntention Decide(Tank self, float elapsedTime)
    {
        var selfPosition3D = self.GetPosition();
        var playerPosition3D = _player.GetPosition();

        var selfPosition = new Vector2(selfPosition3D.X, selfPosition3D.Z);
        var playerPosition = new Vector2(playerPosition3D.X, playerPosition3D.Z);

        var targetPoint = FindTargetPoint(selfPosition, playerPosition);

        var (rotateDirection, moveDirection) = DecideMovement(self, selfPosition, targetPoint, playerPosition);
        var shouldFire = DecideFire(selfPosition, playerPosition, elapsedTime);

        return new TankIntention(rotateDirection, moveDirection, shouldFire);
    }

    // Lane más cercana a MI posición actual (no a la del jugador -- así el cambio
    // de lane es siempre continuo, porque las 3 comparten los mismos extremos).
    // Sobre esa lane, el punto más cercano a la posición del jugador es el objetivo.
    private Vector2 FindTargetPoint(Vector2 selfPosition, Vector2 playerPosition)
    {
        var laneCurves = _battlefield.GetLaneCurves();

        List<Vector2> nearestCurve = laneCurves[0];
        var nearestDistance = float.MaxValue;
        foreach (var curve in laneCurves)
        {
            var distance = Lane.DistanceToPolyline(selfPosition, curve);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestCurve = curve;
            }
        }

        return Lane.ClosestPointOnPolyline(playerPosition, nearestCurve);
    }

    // Gira primero, avanza después (bang-bang): mientras el yaw actual no esté
    // alineado con la dirección al objetivo, solo gira; recién cuando está dentro
    // de la tolerancia, avanza.
    private (int rotateDirection, int moveDirection) DecideMovement(Tank self, Vector2 selfPosition, Vector2 targetPoint, Vector2 playerPosition)
    {
        var toTarget = targetPoint - selfPosition;
        if (toTarget.LengthSquared() < 1e-6f)
        {
            return (0, 0);
        }

        // PlayerTank mapea W (avanzar) a Move(-1), no Move(1) -- el modelo Panzer
        // queda mirando "para atrás" respecto de _world.Forward. Por eso acá se
        // avanza con Move(-1) más abajo, y desiredYaw usa la misma convención que
        // OrbitalCamera para la puntería ((0,0,1), no _world.Forward), que es la
        // que efectivamente hace que el casco quede mirando hacia el objetivo.
        var desiredYaw = MathF.Atan2(toTarget.X, toTarget.Y);
        var currentYaw = self.GetHullYaw();
        var delta = MathHelper.WrapAngle(desiredYaw - currentYaw);

        if (MathF.Abs(delta) > AngleTolerance)
        {
            // Tank.Update hace "_rotation.X -= direction * speed * dt": para
            // INCREMENTAR el yaw actual (delta > 0) hace falta direction negativa.
            var rotateDirection = delta > 0 ? -1 : 1;
            return (rotateDirection, 0);
        }

        // Ya alineado: si está lo bastante cerca del JUGADOR (no del punto sobre
        // la lane, que puede seguir "más allá" de él), frena para no atravesarlo.
        if (Vector2.Distance(selfPosition, playerPosition) <= StopRadius)
        {
            return (0, 0);
        }

        // Move(-1), no Move(1): misma convención que PlayerTank usa para W (ver
        // comentario de desiredYaw más arriba).
        return (0, -1);
    }

    private bool DecideFire(Vector2 selfPosition, Vector2 playerPosition, float elapsedTime)
    {
        _fireTimer += elapsedTime;

        var inRange = Vector2.Distance(selfPosition, playerPosition) <= FireRadius;
        if (!inRange || _fireTimer < FireCooldownSeconds)
        {
            return false;
        }

        _fireTimer = 0f;
        return true;
    }
}
