using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace TGC.MonoGame.TP;

// Responsabilidad: Representar a las cámaras del juego
public class Camera
{
    private Matrix _view;

    private Vector3 _position;
    private Vector3 _target;
    private Vector3 _up;
    
    private float _speed = 25.0f;

    public Camera(Vector3 position,  Vector3 target, Vector3 up)
    {
        _position = position;
        _target = target; 
        _up = up;
    }
    
    public void Initialize()
    {
        _view = Matrix.CreateLookAt(_position, _target, _up);
    }
    
    public void Update(KeyboardState keys, float elapsedTime)
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
        
        
        _view = Matrix.CreateLookAt(_position, _target, _up);
    }

    public Matrix GetView()
    {
        return this._view;
    }

}