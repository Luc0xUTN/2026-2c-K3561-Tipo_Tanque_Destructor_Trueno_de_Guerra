namespace TGC.MonoGame.TP.Tanks.TurretCanonMovementHandlers;

// Responsabilidad: apuntar la torreta y el cañón hacia donde está mirando la cámara, que es lo que
// mueve el mouse. Con el free look activo el cañón se queda clavado en el último punto apuntado.
public class TurretCanonCameraAimHandler : TurretCanonMovementHandler
{
    public void Update(Turret turret, Canon canon, float turretYaw, float aimPitch, bool freeLook)
    {
        if (freeLook)
        {
            return;
        }

        turret.TargetAngle = turretYaw;

        // El cañón usa el signo opuesto al de la elevación de la puntería.
        canon.TargetAngle = -aimPitch;
    }
}
