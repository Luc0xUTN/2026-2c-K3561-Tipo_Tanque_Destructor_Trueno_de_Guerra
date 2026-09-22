using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace TGC.MonoGame.TP.Cameras;

public abstract class BaseCamera
{
    private Matrix _view;
    
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