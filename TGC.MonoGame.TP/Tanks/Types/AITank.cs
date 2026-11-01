using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TGC.MonoGame.TP.Tanks.TurretCanonMovementHandlers;

namespace TGC.MonoGame.TP;

// Responsabilidad: tanque controlado por una AI en vez de por el jugador --
// sirve para los dos bandos (enemigos y aliados), el equipo se recibe como
// parámetro. Le pregunta a AI qué hacer cada tick y aplica el resultado con
// Rotate/Move, heredados de Tank -- AI no puede llamarlos porque son
// protected ahí. La torreta/cañón se apuntan aparte, vía
// TurretCanonAIAimHandler (mismo mecanismo que usa el jugador con el mouse),
// así el casco puede navegar por su cuenta mientras el cañón sigue al
// objetivo real.
public class AITank : Tank
{
    private readonly AI _ai;
    private readonly TurretCanonAIAimHandler _turretCanonAimHandler = new();

    public AITank(Vector3 position, Vector3 scale, Vector3 rotation, AI ai, Battlefield battlefield, Team team)
        : base(position, scale, rotation, battlefield, team)
    {
        _ai = ai;
    }

    public void Update(float elapsedTime, IReadOnlyList<Tank> tanks)
    {
        var intention = _ai.Decide(this, elapsedTime, tanks);

        Rotate(intention.RotateDirection);
        Move(intention.MoveDirection);
        WantsToFire = intention.ShouldFire;

        _turretCanonAimHandler.Update(GetTurret(), GetCanon(), intention.TurretYaw, intention.AimPitch, false);

        base.Update(elapsedTime);
    }
}
