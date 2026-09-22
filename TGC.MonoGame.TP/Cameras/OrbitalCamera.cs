using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace TGC.MonoGame.TP.Cameras;

public class OrbitalCamera : BaseCamera
{
    private Entity _target;
    
    private float _radius;
    private float _yaw; 
    private float _pitch;

    private float _sensitivity = 0.005f;

    private readonly float _centerX; 
    private readonly float _centerY;
    
    public OrbitalCamera(Entity target, float radius, float yaw, float pitch, float centerX, float centerY) 
    {   
        _target = target;
        _yaw = yaw;
        _pitch = pitch;
        _radius = radius;
        _centerX = centerX; 
        _centerY = centerY;
    }

    public override void Initialize()
    {
        this.UpdateView();
    }
    
    private void UpdateView()
    {
        Quaternion rotation = Quaternion.CreateFromYawPitchRoll(_yaw, _pitch, 0);
        
        Vector3 baseOffset = new Vector3(0,0, _radius);
        Vector3 rotatedOffset = Vector3.Transform(baseOffset,rotation);
        
        Vector3 postion = _target.GetPosition() + rotatedOffset;
        
        this.SetView(postion, _target.GetPosition(), Vector3.Up);
    }
    
    public override void Update(KeyboardState keyboardState, MouseState mouseState, float elapsedTime)
    {
        float deltaX = mouseState.X - _centerX;
        float deltaY = mouseState.Y - _centerY;

        _yaw += deltaX * _sensitivity;
        _pitch += deltaY * _sensitivity;
     
        _pitch = MathHelper.Clamp(_pitch, -MathHelper.PiOver2 + 0.01f, MathHelper.PiOver2 - 0.01f);

        this.UpdateView();
    }

    public void SetSensitivity(float sensitivity)
    {
        this._sensitivity = sensitivity;
    }
}