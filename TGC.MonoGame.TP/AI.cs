using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TGC.MonoGame.TP.Tanks;

namespace TGC.MonoGame.TP;

// Lo que decide AI en un tick: AITank es quien la ejecuta, porque Rotate/Move
// son protected en Tank y AI no hereda de ahí. TurretYaw/AimPitch van, sin
// clampear, a TurretCanonAIAimHandler -- Turret/Canon ya clampean solos
// (Canon.GetRotation) si el ángulo pedido se sale de rango.
public readonly struct TankIntention
{
    public int RotateDirection { get; }
    public int MoveDirection { get; }
    public bool ShouldFire { get; }
    public float TurretYaw { get; }
    public float AimPitch { get; }

    public TankIntention(int rotateDirection, int moveDirection, bool shouldFire, float turretYaw, float aimPitch)
    {
        RotateDirection = rotateDirection;
        MoveDirection = moveDirection;
        ShouldFire = shouldFire;
        TurretYaw = turretYaw;
        AimPitch = aimPitch;
    }
}

// Responsabilidad: decidir qué hace un tanque cada tick (perseguir al hostil
// más cercano sin salirse de las lanes navegables, disparar si está cerca). No
// toca Tank directo -- devuelve una intención, AITank la aplica. Sirve para
// cualquier equipo: el "a quién persigo" se resuelve de nuevo cada tick a
// partir de Team, no está atado a un objetivo fijo.
public class AI
{
    // Valores estimados a ojo contra la escala del mapa/tanque (dummy, sin ajustar):
    private const float AngleTolerance = 0.05f; // ~3°, por debajo se considera "ya alineado".
    private const float FireRadius = 40f; // ~3x el ancho de un carril (14u): tope absoluto de disparo (DecideFire) y techo del modo huida.
    private const float FireCooldownSeconds = 2f;

    // Los dos modos usan la MISMA lógica de banda [mínimo, máximo] en
    // DecideMovement (retrocede por debajo del mínimo, avanza por encima del
    // máximo, se planta y dispara en el medio) -- lo único que cambia entre uno
    // y otro son estos cuatro números.
    private const float AggressiveMinRadius = 15f; // ~4x el ancho del tanque (3.69u): no se superpone al objetivo.
    private const float AggressiveMaxRadius = 30f; // ya no cierra sin límite hasta el mínimo: se planta a tirar en cuanto puede.

    // Modo huida: por debajo de este % de vida, en vez de la banda agresiva usa
    // esta, más lejos del objetivo, para seguir disparando desde más lejos sin
    // exponerse.
    private const float FleeHealthRatio = 0.3f;
    private const float FleeMinRadius = 30f; // entre AggressiveMaxRadius (30) y FireRadius (40).

    // Mismos valores que Projectile.cs/GameScene.TrySpawnProjectile -- hacen
    // falta acá para resolver qué ángulo de elevación le pega al objetivo.
    private const float MuzzleSpeed = 400f;
    private const float Gravity = 9.8f;
    private const float MinHorizontalDistance = 0.5f; // evita dividir por ~0 si el objetivo queda casi arriba/abajo.

    // Centro real del BoxCollider del objetivo (Y de 0 a 3, ver Tank.cs) -- el
    // punto al que hay que apuntar, no a target.GetPosition() cruda (que está
    // al ras del piso).
    private const float TargetAimHeightOffset = 1.5f;

    // Pesos de la Utility AI para elegir objetivo entre varios hostiles -- suman
    // 1 para que la utility total quede en [0,1]. Ajustados a ojo: priorizar
    // rematar al débil un poco más que priorizar al más cómodo de alcanzar.
    private const float VictimHealthWeight = 0.6f;
    private const float DistanceWeight = 0.4f;

    // --- FSM Ruta/Combate (Fase C.1) ---
    // Sin esto, el tanque persigue siempre "el punto de mi lane más cercano al
    // objetivo" -- que colapsa al mismo punto para los 3 enemigos apenas el
    // objetivo está cerca de un anclaje de lane (spawn o centro, los 3
    // anclajes se tocan por construcción), y se ve como 3 tanques yendo en
    // línea recta y en paralelo. La solución: dos estados. Ruta = avanzar por
    // los waypoints de MI lane asignada, en la dirección que me toca por Team,
    // sin importar dónde esté nadie. Combate = lo que ya hacía Decide antes de
    // esta fase (Utility AI + bandas + torreta independiente), sin cambios.
    private const float DetectionRadius = 60f; // dispara Ruta -> Combate. Más grande que FireRadius (40): da margen para acercarse antes de estar a tiro.
    private const float LeashRadius = 150f; // correa larga, a propósito (confirmado con Dante) -- más de la mitad de spawnA-spawnB (~252u).
    private const float WaypointArrivalRadius = 5f; // qué tan cerca hay que estar de un waypoint para pasar al siguiente.

    private static readonly TankIntention NoTargetIntention = new(0, 0, false, 0f, 0f);

    // --- Recuperación de bloqueo (issue #11) ---
    // Tank.IsBlocked avisa que el choque de este frame le frenó la mayor
    // parte de la velocidad (ver Tank.ApplyCollisionSpeedScale) -- sin
    // reaccionar a eso, una IA en Ruta hacia un waypoint detrás de un
    // obstáculo mal despejado insiste con el mismo rumbo para siempre
    // (confirmado con una simulación aparte: el clipping de velocidad por sí
    // solo no alcanza si nada deja de pedir "avanzar"). Reacción deliberadamente
    // simple (no es esquiva ni pathfinding, es la versión más chica que corta
    // el ciclo -- a revisar si no alcanza, decisión de Dante 2026-11-02):
    // retrocede un rato fijo, gira un rato fijo (para no quedar reintentando
    // el mismo punto exacto), y recién ahí vuelve a la lógica normal. Mientras
    // dura, tiene prioridad sobre CUALQUIER otra cosa (Ruta o Combate) y no
    // apunta/dispara -- simplificación a propósito, ver comentario de
    // ContinueRecovery.
    private const float BlockedReverseDurationSeconds = 0.75f;
    private const float BlockedRotateDurationSeconds = 0.5f;
    private enum RecoveryPhase { None, Reversing, Rotating }
    private RecoveryPhase _recoveryPhase = RecoveryPhase.None;
    private float _recoveryTimer;

    private readonly Battlefield _battlefield;
    private readonly int? _preferredLaneIndex;

    private float _fireTimer;

    // Estado persistente de la ruta -- se asigna una sola vez (lane) y avanza
    // (waypoint) entre ticks; es lo único que la IA recuerda además del
    // cooldown de disparo.
    private int? _assignedLaneIndex;
    private int _routeWaypointIndex;
    private bool _inCombat;

    // preferredLaneIndex: cuando quien spawnea sabe de antemano cuántos
    // tanques van a convivir y cuántas lanes hay (hoy: 3 enemigos conocidos,
    // 3 lanes, en GameScene), puede asignar una lane DISTINTA a cada uno acá.
    // Sin esto, cada AI elegía independientemente "la lane más cercana a mí" --
    // dos tanques que spawnean cerca entre sí (offsets -10/0, a 10u) podían
    // elegir la MISMA lane y terminar caminando casi superpuestos todo el
    // trayecto (confirmado con datos: >70% del tiempo a menos de 6.4u entre
    // sí, suficiente para chocar y matarse entre ellos antes de llegar al
    // jugador). null deja el comportamiento viejo (útil para spawns que no
    // conocen de antemano a sus compañeros de equipo).
    public AI(Battlefield battlefield, int? preferredLaneIndex = null)
    {
        _battlefield = battlefield;
        _preferredLaneIndex = preferredLaneIndex;
        _fireTimer = 0f;
    }

    // tanks: todos los tanques vivos de la escena (mismo parámetro que ya recibe
    // Projectile.Update) -- de ahí se filtra, cada tick, quién es hostil a self.
    public TankIntention Decide(Tank self, float elapsedTime, IReadOnlyList<Tank> tanks)
    {
        // Prioridad sobre todo lo demás -- ver comentario de los campos de
        // recuperación arriba.
        if (_recoveryPhase != RecoveryPhase.None)
        {
            var ongoingRecovery = ContinueRecovery(elapsedTime);
            if (ongoingRecovery.HasValue)
            {
                return ongoingRecovery.Value;
            }
            // null: la recuperación terminó justo este tick -- sigue de largo
            // a la lógica normal de abajo en vez de perder el tick.
        }
        else if (self.IsBlocked)
        {
            _recoveryPhase = RecoveryPhase.Reversing;
            _recoveryTimer = BlockedReverseDurationSeconds;
            return new TankIntention(0, 1, false, 0f, 0f); // moveDirection=1: retroceder (misma convención que DecideMovement).
        }

        var selfPosition3D = self.GetPosition();
        var selfPosition = new Vector2(selfPosition3D.X, selfPosition3D.Z);

        // Si no queda NINGÚN hostil vivo en todo el mapa (sin límite de
        // distancia), no hay nada que decidir -- partida resuelta para este
        // bando. Distinto de "no hay nadie CERCA", que es el caso normal de Ruta.
        if (FindBestHostile(self, selfPosition, tanks, float.MaxValue) == null)
        {
            return NoTargetIntention;
        }

        // Mientras ya está en Combate, usa la correa (más laxo); recién
        // buscando a quién entrarle, usa la detección (más corto).
        var searchRadius = _inCombat ? LeashRadius : DetectionRadius;
        var target = FindBestHostile(self, selfPosition, tanks, searchRadius);

        if (target == null)
        {
            // Primera vez, o recién se salió de Combate (sin hostil dentro de
            // la correa): (re)sincronizar la ruta a la posición real, no a
            // dónde "debería" estar si nunca hubiese entrado en combate.
            if (_inCombat || _assignedLaneIndex == null)
            {
                AssignLaneAndResyncWaypoint(selfPosition);
            }

            _inCombat = false;
            var (routeRotate, routeMove) = DecideRoute(self, selfPosition);
            return new TankIntention(routeRotate, routeMove, false, 0f, 0f);
        }

        _inCombat = true;

        var targetPosition3D = target.GetPosition();
        var targetPosition = new Vector2(targetPosition3D.X, targetPosition3D.Z);
        var isFleeing = self.HealthRatio <= FleeHealthRatio;

        var targetPoint = FindTargetPoint(selfPosition, targetPosition);
        var (rotateDirection, moveDirection) = DecideMovement(self, selfPosition, targetPoint, targetPosition, isFleeing);

        // Para el pitch, apuntar al CENTRO del casco del objetivo, no a su base
        // (target.GetPosition() está al ras del piso -- Tank.cs la fija a la
        // altura del terreno). TargetAimHeightOffset = 1.5 es el centro real del
        // BoxCollider del tanque (Y de 0 a 3, BoxColliderFactory.CreateBox en
        // Tank.cs). Sin esto, a corta distancia el ángulo requerido apunta al
        // piso frente al objetivo en vez de a su casco.
        var aimPoint3D = targetPosition3D + Vector3.UnitY * TargetAimHeightOffset;
        var (turretYaw, aimPitch, canHitTarget) = DecideAim(self, aimPoint3D);
        var shouldFire = DecideFire(selfPosition, targetPosition, elapsedTime, canHitTarget);

        return new TankIntention(rotateDirection, moveDirection, shouldFire, turretYaw, aimPitch);
    }

    // Avanza la recuperación de bloqueo un tick. Devuelve la intención a usar
    // mientras sigue en curso, o null el tick en que recién termina (para que
    // Decide caiga a la lógica normal en el MISMO tick, en vez de perder uno
    // quieto). No apunta ni dispara mientras dura -- simplificación a
    // propósito: es una reacción mecánica corta (menos de 1.5s en total), no
    // vale la pena todavía recalcular puntería real durante ella.
    private TankIntention? ContinueRecovery(float elapsedTime)
    {
        _recoveryTimer -= elapsedTime;

        if (_recoveryPhase == RecoveryPhase.Reversing)
        {
            if (_recoveryTimer <= 0f)
            {
                _recoveryPhase = RecoveryPhase.Rotating;
                _recoveryTimer = BlockedRotateDurationSeconds;
            }
            return new TankIntention(0, 1, false, 0f, 0f); // retrocede.
        }

        // Rotating.
        if (_recoveryTimer <= 0f)
        {
            _recoveryPhase = RecoveryPhase.None;
            return null;
        }
        return new TankIntention(1, 0, false, 0f, 0f); // gira sobre el lugar, sentido fijo -- no hace falta variar, alcanza con no reintentar derecho al mismo punto.
    }

    // Utility AI: entre los hostiles vivos DENTRO de maxDistance, el de mayor
    // utilidad = suma pesada de factores normalizados a [0,1] (vida de la
    // víctima, qué tan cerca está). Se recalcula desde cero cada tick (no se
    // recuerda el objetivo anterior), así que si el elegido muere o aparece
    // uno mejor, el próximo tick lo nota solo. maxDistance=float.MaxValue
    // equivale a "¿hay alguien vivo en todo el mapa?", sin filtro de distancia.
    private static Tank FindBestHostile(Tank self, Vector2 selfPosition, IReadOnlyList<Tank> tanks, float maxDistance)
    {
        Tank best = null;
        var bestUtility = float.NegativeInfinity;

        foreach (var tank in tanks)
        {
            if (tank.IsDead || tank.Team == self.Team) continue;

            var tankPosition3D = tank.GetPosition();
            var tankPosition = new Vector2(tankPosition3D.X, tankPosition3D.Z);
            var distance = Vector2.Distance(selfPosition, tankPosition);
            if (distance > maxDistance) continue;

            var victimHealthScore = 1f - tank.HealthRatio; // más bajo su vida, más conviene rematarlo.
            var distanceScore = 1f - MathHelper.Clamp(distance / GameSettings.MapSize, 0f, 1f); // más cerca, mejor.

            var utility = VictimHealthWeight * victimHealthScore + DistanceWeight * distanceScore;

            if (utility > bestUtility)
            {
                bestUtility = utility;
                best = tank;
            }
        }

        return best;
    }

    // Lane asignada UNA SOLA VEZ (la más cercana a MI posición en ese momento,
    // nunca más se recalcula -- evita que cerca de un anclaje, donde varias
    // lanes casi se tocan, el tanque "salte" de una a otra sin aviso) y el
    // waypoint de esa lane más cercano a la posición actual. Se llama al
    // primer tick y cada vez que se vuelve de Combate a Ruta.
    private void AssignLaneAndResyncWaypoint(Vector2 selfPosition)
    {
        if (_assignedLaneIndex == null)
        {
            _assignedLaneIndex = _preferredLaneIndex ?? FindNearestLaneIndex(selfPosition);
        }

        _routeWaypointIndex = NearestWaypointIndex(selfPosition, _assignedLaneIndex.Value);
    }

    private int FindNearestLaneIndex(Vector2 position)
    {
        var laneCurves = _battlefield.GetLaneCurves();

        var bestLaneIndex = 0;
        var bestDistance = float.MaxValue;
        for (var i = 0; i < laneCurves.Count; i++)
        {
            var distance = Lane.DistanceToPolyline(position, laneCurves[i]);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestLaneIndex = i;
            }
        }

        return bestLaneIndex;
    }

    private int NearestWaypointIndex(Vector2 position, int laneIndex)
    {
        var curve = _battlefield.GetLaneCurves()[laneIndex];

        var bestIndex = 0;
        var bestDistance = float.MaxValue;
        for (var i = 0; i < curve.Count; i++)
        {
            var distance = Vector2.Distance(position, curve[i]);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    // Estado Ruta: avanza por los waypoints de la lane asignada, en la
    // dirección que le toca por Team (las lanes están ordenadas spawnA ->
    // centro -> spawnB; Enemy decrece el índice hacia spawnA, Allied lo
    // incrementaría hacia spawnB). Nunca mira a ningún objetivo -- por eso no
    // puede colapsar en los anclajes como pasaba antes. Avanza el índice todo
    // lo que el radio de llegada permita en un solo tick (los puntos de la
    // curva están MUY juntos entre sí, a menos de 1u -- si solo avanzara de
    // uno en uno, el waypoint objetivo se quedaría atrás de la posición real).
    private (int rotateDirection, int moveDirection) DecideRoute(Tank self, Vector2 selfPosition)
    {
        var curve = _battlefield.GetLaneCurves()[_assignedLaneIndex.Value];
        var directionStep = self.Team == Team.Enemy ? -1 : 1;

        while (true)
        {
            var nextIndex = _routeWaypointIndex + directionStep;
            if (nextIndex < 0 || nextIndex >= curve.Count) break;
            if (Vector2.Distance(selfPosition, curve[_routeWaypointIndex]) > WaypointArrivalRadius) break;

            _routeWaypointIndex = nextIndex;
        }

        var waypoint = curve[_routeWaypointIndex];
        var toWaypoint = waypoint - selfPosition;
        if (toWaypoint.LengthSquared() < 1e-6f)
        {
            return (0, 0);
        }

        // Misma convención que DecideMovement (ver comentario de desiredYaw ahí).
        var desiredYaw = MathF.Atan2(toWaypoint.X, toWaypoint.Y);
        var currentYaw = self.GetHullYaw();
        var delta = MathHelper.WrapAngle(desiredYaw - currentYaw);

        if (MathF.Abs(delta) > AngleTolerance)
        {
            var rotateDirection = delta > 0 ? -1 : 1;
            return (rotateDirection, 0);
        }

        return (0, -1);
    }

    // Lane más cercana a MI posición actual (no a la del objetivo -- así el cambio
    // de lane es siempre continuo, porque las 3 comparten los mismos extremos).
    // Sobre esa lane, el punto más cercano a la posición del objetivo es el destino.
    private Vector2 FindTargetPoint(Vector2 selfPosition, Vector2 targetPosition)
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

        return Lane.ClosestPointOnPolyline(targetPosition, nearestCurve);
    }

    // Gira primero, avanza después (bang-bang): mientras el yaw actual no esté
    // alineado con la dirección al objetivo, solo gira; recién cuando está dentro
    // de la tolerancia, avanza (o retrocede, en modo huida). El casco sigue
    // navegando por la lane igual que siempre -- ya NO hace falta que quede
    // mirando al objetivo para poder disparar, porque la torreta apunta
    // independiente (ver DecideAim); acá solo importa para decidir el rumbo.
    private (int rotateDirection, int moveDirection) DecideMovement(Tank self, Vector2 selfPosition, Vector2 targetPoint, Vector2 targetPosition, bool isFleeing)
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
            // Rotate(1) termina restando al yaw (ver Tank.Update: un rotationDirection
            // positivo frena la oruga izquierda y acelera la derecha, lo que achica
            // el yaw) -- para INCREMENTAR el yaw actual (delta > 0) hace falta
            // rotateDirection negativo.
            var rotateDirection = delta > 0 ? -1 : 1;
            return (rotateDirection, 0);
        }

        // Ya alineado con el rumbo. Distancia al OBJETIVO real (no al punto sobre
        // la lane, que puede seguir "más allá" de él).
        var distanceToTarget = Vector2.Distance(selfPosition, targetPosition);

        // Misma banda [mínimo, máximo] para los dos modos -- solo cambian los
        // números (ver AggressiveMinRadius/AggressiveMaxRadius/FleeMinRadius/FireRadius).
        var minRadius = isFleeing ? FleeMinRadius : AggressiveMinRadius;
        var maxRadius = isFleeing ? FireRadius : AggressiveMaxRadius;

        if (distanceToTarget < minRadius)
        {
            // Retrocede: Move(1) es la dirección opuesta a "avanzar" (ver
            // comentario de desiredYaw), así que con el casco igual de
            // alineado, esto aumenta la distancia al objetivo en vez de
            // achicarla.
            return (0, 1);
        }

        if (distanceToTarget > maxRadius)
        {
            // Move(-1), no Move(1): misma convención que PlayerTank usa para W
            // (ver comentario de desiredYaw más arriba).
            return (0, -1);
        }

        // Zona cómoda entre el mínimo y el máximo: se planta y dispara.
        return (0, 0);
    }

    // Ángulo de torreta (yaw, relativo al casco) y de cañón (pitch, elevación en
    // espacio de mundo) para que la torreta apunte al objetivo real sin importar
    // hacia dónde esté yendo el casco -- mismo mecanismo que ya usa
    // TurretCanonCameraAimHandler para el jugador (Turret/Canon.TargetAngle).
    private (float turretYaw, float aimPitch, bool canHitTarget) DecideAim(Tank self, Vector3 targetPosition3D)
    {
        var muzzlePosition = self.GetCanonWorldTransform().Translation;

        var horizontalDelta = new Vector2(targetPosition3D.X - muzzlePosition.X, targetPosition3D.Z - muzzlePosition.Z);
        var horizontalDistance = horizontalDelta.Length();
        var heightDelta = targetPosition3D.Y - muzzlePosition.Y;

        var desiredWorldYaw = MathF.Atan2(horizontalDelta.X, horizontalDelta.Y);
        var turretYaw = MathHelper.WrapAngle(desiredWorldYaw - self.GetHullYaw());

        var hasSolution = TrySolveBallisticPitch(horizontalDistance, heightDelta, out var aimPitch);
        var canHitTarget = hasSolution
            && aimPitch >= Canon.MinimumAimPitch
            && aimPitch <= Canon.MaximumAimPitch;

        return (turretYaw, aimPitch, canHitTarget);
    }

    // Despeja el ángulo de elevación (theta) de la ecuación de tiro parabólico
    // y = x*tan(theta) - g*x^2 / (2*v0^2*cos^2(theta)) para un v0/g dados --
    // mismos valores que Projectile.cs/TrySpawnProjectile. Sustituyendo
    // u = tan(theta) y 1/cos^2(theta) = 1 + u^2 queda una cuadrática en u:
    // A*u^2 - x*u + (y + A) = 0, con A = g*x^2/(2*v0^2). Tiene dos soluciones
    // (tiro rasante y tiro de obús); acá se usa siempre la rasante (la raíz "-"),
    // la única que tiene sentido para un cañón de tanque.
    private static bool TrySolveBallisticPitch(float horizontalDistance, float heightDelta, out float pitch)
    {
        pitch = 0f;

        if (horizontalDistance < MinHorizontalDistance)
        {
            // Objetivo casi directamente arriba/abajo: este modelo no da un
            // ángulo razonable (tendería a vertical), y un cañón de tanque no
            // puede apuntar así de todos modos.
            return false;
        }

        var a = Gravity * horizontalDistance * horizontalDistance / (2f * MuzzleSpeed * MuzzleSpeed);
        var discriminant = horizontalDistance * horizontalDistance - 4f * a * (heightDelta + a);
        if (discriminant < 0f)
        {
            // Fuera del alcance máximo para esta velocidad de boca -- no existe
            // ángulo que llegue (en la práctica no pasa dentro de FireRadius).
            return false;
        }

        var u = (horizontalDistance - MathF.Sqrt(discriminant)) / (2f * a);
        pitch = MathF.Atan(u);
        return true;
    }

    private bool DecideFire(Vector2 selfPosition, Vector2 targetPosition, float elapsedTime, bool canHitTarget)
    {
        _fireTimer += elapsedTime;

        var inRange = Vector2.Distance(selfPosition, targetPosition) <= FireRadius;
        if (!inRange || !canHitTarget || _fireTimer < FireCooldownSeconds)
        {
            return false;
        }

        _fireTimer = 0f;
        return true;
    }
}
