using Microsoft.Xna.Framework;

namespace TGC.MonoGame.TP;

// Responsabilidad: tanque controlado por una AI en vez de por el jugador. Le
// pregunta a AI qué hacer cada tick y aplica el resultado con Rotate/Move,
// heredados de Tank -- AI no puede llamarlos porque son protected ahí.
public class EnemyTank : Tank
{
    private readonly AI _ai;

    public EnemyTank(Vector3 position, Vector3 scale, Vector3 rotation, Color color, AI ai, Battlefield battlefield) 
        : base(position, scale, rotation, color, battlefield)
    {
        _ai = ai;
    }

    public override void Update(float elapsedTime)
    {
        var intention = _ai.Decide(this, elapsedTime);

        Rotate(intention.RotateDirection);
        Move(intention.MoveDirection);
        WantsToFire = intention.ShouldFire;

        base.Update(elapsedTime);
    }
}
