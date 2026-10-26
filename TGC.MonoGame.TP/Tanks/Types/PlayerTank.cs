using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace TGC.MonoGame.TP;

// Responsabilidad: Representar a un tanque jugador
public class PlayerTank: Tank
{
    // Más rápido que el de la IA (AI.cs sigue en 2s) -- el jugador se quejó de
    // que la cadencia se sentía desesperantemente lenta. Provisorio, ajustar por feel.
    private const float FireCooldownSeconds = 0.5f;

    private Keys _keyForward = Keys.W;
    private Keys _keyBackward = Keys.S;
    private Keys _keyRotateLeft = Keys.A;
    private Keys _keyRotateRight = Keys.D;

    private ButtonState _previousFireButtonState = ButtonState.Released;
    private float _fireTimer = FireCooldownSeconds;

    public PlayerTank(Vector3 position, Vector3 scale, Vector3 rotation, Battlefield battlefield) : base(position, scale, rotation, battlefield)
    {
    }


    public override void Update(float elapsedTime)
    {
        KeyboardState keyboardState = Keyboard.GetState();

        if (keyboardState.IsKeyDown(_keyRotateRight))
            Rotate(1);
        else if (keyboardState.IsKeyDown(_keyRotateLeft))
            Rotate(-1);
        else
        {
            Rotate(0);
            if (keyboardState.IsKeyDown(_keyForward))
                Move(-1);
            else if (keyboardState.IsKeyDown(_keyBackward))
                Move(1);
            else
                Move(0);
        }

        UpdateFire(elapsedTime);

        base.Update(elapsedTime);
    }

    private void UpdateFire(float elapsedTime)
    {
        _fireTimer += elapsedTime;

        var fireButtonState = Mouse.GetState().LeftButton;
        // Flanco de bajada a subida (click, no mantener apretado) -- si no, con
        // el botón mantenido dispararía una vez por frame en cuanto zafe el cooldown.
        var justPressed = fireButtonState == ButtonState.Pressed && _previousFireButtonState == ButtonState.Released;
        _previousFireButtonState = fireButtonState;

        if (justPressed && _fireTimer >= FireCooldownSeconds)
        {
            WantsToFire = true;
            _fireTimer = 0f;
        }
        else
        {
            WantsToFire = false;
        }
    }
}
