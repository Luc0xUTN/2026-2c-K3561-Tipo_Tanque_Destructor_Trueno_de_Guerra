using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP.Tanks;


public class Turret : RotationalObject
{
    public Turret(ModelBone bone) : base(bone)
    {
        _angularAceleration = 10f; 
        _angularDamping = 5.0f;
    }

    protected override Quaternion GetRotation()
    {
        return Quaternion.CreateFromYawPitchRoll(_angle, 0, 0);
    }
}