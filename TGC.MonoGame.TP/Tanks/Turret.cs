using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP.Tanks;


public class Turret : RotationalObject
{
    public Turret(ModelBone bone) : base(bone)
    {
        // Giro lento y con inercia, : la torreta persigue la mira en vez de
        // clavarse instantáneamente en la puntería del mouse.
        _angularAceleration = 5f;
        _angularDamping = 4.0f;
    }

    protected override Quaternion GetRotation()
    {
        return Quaternion.CreateFromYawPitchRoll(_angle, 0, 0);
    }
}