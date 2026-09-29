using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace TGC.MonoGame.TP.Tanks.TurretCanonMovementHandlers;

public class TurretCanonMouseHandler : TurretCanonMovementHandler
{
    private readonly float _centerX;
    private readonly float _centerY;
    private readonly float _sensitivity;

    private readonly float _offset = (float) Math.PI;
    
    private float _yaw = 0; 
    private float _pitch = 0;
    
    private readonly float _minPitch = -0.17f; 
    private readonly float _maxPitch = 0.43f;
    public TurretCanonMouseHandler(float centerX, float centerY, float sensitivity)
    {
        _centerX = centerX;
        _centerY = centerY;
        _sensitivity = sensitivity; 
    }
    
    public void Update(Turret turret, Canon canon)
    {
        MouseState mouseState = Mouse.GetState();
        
        float deltaX = mouseState.X - _centerX;
        float deltaY = mouseState.Y - _centerY;

        
        _yaw += deltaX * _sensitivity;
        _pitch += deltaY * _sensitivity;

        // _pitch = MathHelper.Clamp(_pitch, _minPitch, _maxPitch);
        
        if (deltaX != 0 || deltaY != 0)
        {
            turret.TargetAngle = _yaw + _offset; 
            canon.TargetAngle = -_pitch;
        }
    }
}