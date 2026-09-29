namespace TGC.MonoGame.TP.Tanks.TurretCanonMovementHandlers;

public interface TurretCanonMovementHandler
{
    /// <summary>
    ///     Aplica la puntería actual a la torreta y al cañón.
    /// </summary>
    /// <param name="turretYaw">Yaw de puntería en espacio de mundo.</param>
    /// <param name="aimPitch">Elevación de la puntería en espacio de mundo (positivo = hacia arriba).</param>
    /// <param name="freeLook">
    ///     Si está activo la cámara se mueve sola y la torreta y el cañón no deben seguirla.
    /// </param>
    void Update(Turret turret, Canon canon, float turretYaw, float aimPitch, bool freeLook);
}
