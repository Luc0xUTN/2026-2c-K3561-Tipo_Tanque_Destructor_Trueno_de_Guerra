using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TGC.MonoGame.TP.Cameras;

namespace TGC.MonoGame.TP.scenes;

public class MenuScene : Scene
{

    private Battlefield _battlefield;
    
    private BaseCamera _camera;
    
    private GraphicsDevice _device;
    private ContentManager _contentManager;
    private Game _game; 
    
    private Matrix _projection;
    
    public MenuScene(Game game)
    {
        _game = game; 
    }


    private bool _flTransition = false;
    
    public void Initialize()
    {
        _contentManager = new ContentManager(_game.Services, "Content");
        _device = _game.Services.GetService<IGraphicsDeviceService>().GraphicsDevice;

        Vector3 camPosition = new Vector3(150, 200, 150);
        
        _camera = new IsometricCamera(camPosition, Vector3.Zero, Vector3.Up);
        
        _battlefield = new Battlefield(ContentPaths.ContentFolderEffects + "BasicShader", Color.Green);
        _battlefield.Initialize(_device, GameSettings.MapSize); 
        
        int farPlaneDistance = (int) Math.Ceiling(Math.Sqrt(Math.Pow(GameSettings.MapSize, 2) + Math.Pow(GameSettings.MapSize, 2))); 
        _projection =
            Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, _device.Viewport.AspectRatio, 1, farPlaneDistance);
        
        _game.IsMouseVisible = true; 
    }

    public void LoadContent()
    {
        _battlefield.LoadContent(_game.Content, ContentPaths.ContentFolder3D); // Lo cargo al contenido global debido a que no se debería destruir en ningún momento
        
    }

    public void Update(float elapsedTime)
    {
        KeyboardState keyboardState = Keyboard.GetState();
        MouseState mouseState = Mouse.GetState();


        if (keyboardState.GetPressedKeyCount() > 0)
        {
            _flTransition = true;
        }
        
        _camera.Update(elapsedTime);
    }

    public void Draw()
    {
        _battlefield.Draw(_device, _camera.GetView(),  _projection);
    }

    public void Unload()
    {
        _contentManager.Unload();
    }

    public bool ShouldTransition()
    {
        return _flTransition;
    }

    public Scene Transition()
    {
        this.Unload();
        
        GameScene gameScene = new GameScene(_game); 
        gameScene.SetBattlefield(_battlefield);
        
        return gameScene;
    }
}