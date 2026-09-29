using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TGC.MonoGame.TP.Tanks;

namespace TGC.MonoGame.TP.Cameras;

// Responsabilidad: seguir al tanque y definir hacia dónde se está apuntando.
// Es la única que consume el mouse: el juego le pasa la puntería resultante a la torreta y al cañón.
public class OrbitalCamera : BaseCamera
{
    private readonly Entity _target;
    
    private readonly float _radius;

    private readonly float _centerX; 
    private readonly float _centerY;
    
    private float _sensitivity;

    public OrbitalCamera(Entity target, float radius, float centerX, float centerY, float sensitivity) 
    {   
        _target = target;
        _radius = radius;
        _centerX = centerX; 
        _centerY = centerY;
        _sensitivity = sensitivity;
    }

    public override void Initialize()
    {
        this.UpdateView();
    }
    
    private void UpdateView()
    {
        // La cámara se ubica sobre la línea de puntería, detrás del tanque, mirando hacia donde se apunta.
        Quaternion rotation = Quaternion.CreateFromYawPitchRoll(MathHelper.WrapAngle(Yaw + MathHelper.Pi), Pitch, 0);
        
        Vector3 position = _target.GetPosition() + Vector3.Transform(new Vector3(0, 0, _radius), rotation);
        
        this.SetView(position, _target.GetPosition(), Vector3.Up);
    }
    
    public override void Update(KeyboardState keyboardState, MouseState mouseState, float elapsedTime)
    {
        // El mouse está capturado y el juego lo recentera en cada frame, así que este delta es
        // el movimiento realizado durante el frame.
        float deltaX = mouseState.X - _centerX;
        float deltaY = mouseState.Y - _centerY;

        Yaw = MathHelper.WrapAngle(Yaw + deltaX * _sensitivity);
        Pitch -= deltaY * _sensitivity;

        // Con el free look se puede mirar en cualquier dirección. En el resto del tiempo la puntería
        // queda acotada al rango de elevación real del cañón, para que la cámara nunca se vaya
        // más lejos de donde el cañón puede llegar.
        Pitch = IsFreeLook
            ? MathHelper.Clamp(Pitch, -MathHelper.PiOver2 + 0.01f, MathHelper.PiOver2 - 0.01f)
            : Math.Clamp(Pitch, Canon.MinimumAimPitch, Canon.MaximumAimPitch);

        this.UpdateView();
    }

    public void SetSensitivity(float sensitivity)
    {
        this._sensitivity = sensitivity;
    }
}
