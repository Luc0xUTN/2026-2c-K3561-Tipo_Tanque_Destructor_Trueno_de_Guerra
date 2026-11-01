namespace TGC.MonoGame.TP.Tanks.TurretCanonMovementHandlers;

// Responsabilidad: apuntar la torreta y el cañón hacia donde los calculó AI.cs
// (DecideAim) -- mismo contrato que TurretCanonCameraAimHandler, pero la
// fuente de turretYaw/aimPitch es la IA en vez del mouse/cámara. No hay free
// look para un tanque enemigo, así que ese parámetro no aplica.
public class TurretCanonAIAimHandler : TurretCanonMovementHandler
{
    public void Update(Turret turret, Canon canon, float turretYaw, float aimPitch, bool freeLook)
    {
        turret.TargetAngle = turretYaw;

        // El cañón usa el signo opuesto al de la elevación de la puntería.
        canon.TargetAngle = -aimPitch;
    }
}
