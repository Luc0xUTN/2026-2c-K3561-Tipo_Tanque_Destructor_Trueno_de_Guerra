using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace TGC.MonoGame.TP.Cameras;

public abstract class BaseCamera
{
    private Matrix _view;

    /// <summary>
    ///     Dirección de puntería en espacio de mundo. Es la que mueve el mouse y, salvo durante el
    ///     free look, también es hacia donde apuntan la torreta y el cañón del tanque.
    /// </summary>
    public float Yaw { get; protected set; }

    /// <summary>
    ///     Elevación de la puntería en espacio de mundo, alrededor del eje X (positivo = hacia arriba).
    /// </summary>
    public float Pitch { get; protected set; }

    /// <summary>
    ///     Mientras está activo el mouse mueve la cámara y la torreta y el cañón quedan clavados en el
    ///     último punto apuntado (Free Look). Al desactivarlo vuelven a apuntar a la dirección de puntería.
    /// </summary>
    public bool IsFreeLook { get; set; }

    /// <summary>
    ///     Dirección unitaria hacia donde apunta el mouse, en espacio de mundo. La usa el HUD para
    ///     proyectar el retículo sobre la pantalla. Misma convención que el cañón: a yaw 0 apunta a
    ///     +Z y un pitch positivo mira hacia arriba.
    /// </summary>
    public Vector3 AimDirection
    {
        get
        {
            float cosPitch = MathF.Cos(Pitch);
            return new Vector3(MathF.Sin(Yaw) * cosPitch, MathF.Sin(Pitch), MathF.Cos(Yaw) * cosPitch);
        }
    }

    public abstract void Initialize();
    
    public abstract void Update(float elapsedTime);
    
    
    public void SetView(Vector3 position, Vector3 target, Vector3 up)
    {
        _view = Matrix.CreateLookAt(position, target, up);
    }
    
    public Matrix GetView()
    {
        return _view;
    }
}