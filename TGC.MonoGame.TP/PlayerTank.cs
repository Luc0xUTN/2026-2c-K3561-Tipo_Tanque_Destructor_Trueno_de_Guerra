using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace TGC.MonoGame.TP;

// Responsabilidad: Representar a un tanque jugador
public class PlayerTank(Vector3 position, Vector3 scale, Vector3 rotation, Color color) : Tank(position, scale, rotation, color)
{
    private Keys _keyForward = Keys.W;
    private Keys _keyBackward = Keys.S;
    private Keys _keyRotateLeft = Keys.A;
    private Keys _keyRotateRight = Keys.D;

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
                Move(1);
            else if (keyboardState.IsKeyDown(_keyBackward))
                Move(-1);
            else
                Move(0);
        }
            
        

        base.Update(elapsedTime);
    }
}