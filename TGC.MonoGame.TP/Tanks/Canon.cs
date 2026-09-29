using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP.Tanks;

public class Canon : RotationalObject
{
    private const float MaximumAngle = 0.17f; 
    private const float MinimumAngle = -0.43f;
    
    public Canon(ModelBone bone) : base(bone)
    {
        _angularAceleration = 7f;
        _angularDamping = 5.0f;
    }
    
    protected override Quaternion GetRotation()
    {
        _angle = Math.Clamp(_angle, MinimumAngle, MaximumAngle);
        
        TargetAngle = Math.Clamp(TargetAngle, MinimumAngle, MaximumAngle);
        
        return Quaternion.CreateFromYawPitchRoll(0, _angle, 0);
    }
}