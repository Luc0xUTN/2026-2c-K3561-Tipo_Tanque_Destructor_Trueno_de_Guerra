using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP.Tanks;

public class Canon : RotationalObject
{
    // El cañón usa el signo opuesto a la elevación de la puntería: negativo es hacia arriba.
    public const float MaximumAngle = 0.17f;
    public const float MinimumAngle = -0.43f;

    // Mismo rango expresado como elevación de la puntería en espacio de mundo (positivo = hacia arriba).
    // La cámara lo usa para no dejar que la puntería se vaya fuera del alcance real del cañón.
    public const float MinimumAimPitch = -MaximumAngle;
    public const float MaximumAimPitch = -MinimumAngle;

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