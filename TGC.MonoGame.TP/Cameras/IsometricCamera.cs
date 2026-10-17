using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TGC.MonoGame.TP.Cameras;

namespace TGC.MonoGame.TP;

// Responsabilidad: Representar a las cámaras del juego
public class IsometricCamera : BaseCamera
{
    private const float _angularSpeed = 0.3f; 
    
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
    
    public override void Update(float elapsedTime)
    {
        
        float difAngle = _angularSpeed * elapsedTime;
        
        Quaternion rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, difAngle); 
        
        _position = Vector3.Transform(_position, Matrix.CreateFromQuaternion(rotation)); 
        
        SetView(_position, _target, _up);
    }
    
}