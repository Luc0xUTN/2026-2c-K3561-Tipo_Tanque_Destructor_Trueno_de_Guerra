using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TGC.MonoGame.TP.Cameras;

namespace TGC.MonoGame.TP;

// Responsabilidad: Representar a las cámaras del juego
public class IsometricCamera : BaseCamera
{
    private Vector3 _position;
    private Vector3 _target;
    private Vector3 _up;
    
    private float _speed = 25.0f;

    public IsometricCamera(Vector3 position,  Vector3 target, Vector3 up)
    {
        _position = position;
        _target = target; 
        _up = up;
    }
    
    public override void Initialize()
    {
        SetView(_position, _target, _up);
    }
    
    public override void Update(KeyboardState keys, MouseState mouseState, float elapsedTime)
    {
        Vector3 cameraVelocity =  _speed * elapsedTime * Vector3.One;
        
        if (keys.IsKeyDown(Keys.Down))
        {
            _position += cameraVelocity;
        }
        else if (keys.IsKeyDown(Keys.Up))
        {
            _position -= cameraVelocity;
        }
        
        SetView(_position, _target, _up);
    }
    
}