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

    public abstract void Initialize();
    
    public abstract void Update(KeyboardState keyboardState, MouseState mouseState, float elapsedTime);
    
    
    public void SetView(Vector3 position, Vector3 target, Vector3 up)
    {
        _view = Matrix.CreateLookAt(position, target, up);
    }
    
    public Matrix GetView()
    {
        return _view;
    }
}