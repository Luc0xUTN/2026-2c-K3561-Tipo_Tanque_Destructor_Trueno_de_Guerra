using System;
using Microsoft.Xna.Framework.Input;

namespace TGC.MonoGame.TP.Tanks.TurretCanonMovementHandlers;

public class TurretCanonKeyboardHandler : TurretCanonMovementHandler
{
    private Keys _keyRotateTurretLeft = Keys.Left;
    private Keys _keyRotateTurretRight = Keys.Right;

    private Keys _keyRotateUpCanon = Keys.Up;
    private Keys _keyRotateDownCanon = Keys.Down;

    public TurretCanonKeyboardHandler()
    {
        
    }
    
    public TurretCanonKeyboardHandler(Keys keyRotateTurretLeft, Keys keyRotateTurretRight, Keys keyRotateUpCanon, Keys keyRotateDownCanon)
    {
        _keyRotateTurretLeft = keyRotateTurretLeft;
        _keyRotateTurretRight = keyRotateTurretRight;
        _keyRotateUpCanon = keyRotateUpCanon;
        _keyRotateDownCanon = keyRotateDownCanon;
        
    }

    public void Update( Turret turret, Canon canon, float turretYaw, float aimPitch, bool freeLook)
    {
        if (freeLook)
        {
            return;
        }

        KeyboardState keyboardState = Keyboard.GetState();
        
        if (keyboardState.IsKeyDown(_keyRotateTurretLeft))
        {
            turret.TargetAngle += 0.05f;
        }
        else if (keyboardState.IsKeyDown(_keyRotateTurretRight))
        {
            turret.TargetAngle -= 0.05f;
        }

        if (keyboardState.IsKeyDown(_keyRotateUpCanon))
        {
            canon.TargetAngle -= 0.05f;
        }
        else if (keyboardState.IsKeyDown(_keyRotateDownCanon))
        {
            canon.TargetAngle += 0.05f;
        }

    }
    
}