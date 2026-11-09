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

    // God mode: cheat de demo para el jugador -- sin cooldown de disparo y
    // sin recibir daño (IsInvincible, en Tank). No toca la física del choque:
    // seguís clippeando velocidad contra obstáculos, solo no te resta vida.
    private readonly Keys _keyGodModeModifier = Keys.LeftControl;
    private readonly Keys _keyGodModeToggle = Keys.G;
    private bool _previousGodModeComboPressed;

    public bool IsGodMode { get; private set; }

    // Para el HUD (barra de reload): 0 recién disparado, 1 listo para
    // disparar de nuevo. Mismo truco de cast a float que HealthRatio.
    public float FireCooldownRatio => MathHelper.Clamp(_fireTimer / FireCooldownSeconds, 0f, 1f);

    public PlayerTank(Vector3 position, Vector3 scale, Vector3 rotation, Battlefield battlefield) : base(position, scale, rotation, battlefield, Team.Allied)
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

        UpdateGodMode(keyboardState);
        UpdateFire(elapsedTime);

        base.Update(elapsedTime);
    }

    private void UpdateGodMode(KeyboardState keyboardState)
    {
        var comboPressed = keyboardState.IsKeyDown(_keyGodModeModifier) && keyboardState.IsKeyDown(_keyGodModeToggle);
        // Mismo patrón de flanco que el botón de disparo -- si no, mantener
        // las dos teclas apretadas un instante de más prendería y apagaría
        // el modo varias veces en los mismos frames.
        if (comboPressed && !_previousGodModeComboPressed)
        {
            IsGodMode = !IsGodMode;
            IsInvincible = IsGodMode;
        }
        _previousGodModeComboPressed = comboPressed;
    }

    private void UpdateFire(float elapsedTime)
    {
        _fireTimer += elapsedTime;

        var fireButtonState = Mouse.GetState().LeftButton;
        // Flanco de bajada a subida (click, no mantener apretado) -- si no, con
        // el botón mantenido dispararía una vez por frame en cuanto zafe el cooldown.
        var justPressed = fireButtonState == ButtonState.Pressed && _previousFireButtonState == ButtonState.Released;
        _previousFireButtonState = fireButtonState;

        if (justPressed && (IsGodMode || _fireTimer >= FireCooldownSeconds))
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
