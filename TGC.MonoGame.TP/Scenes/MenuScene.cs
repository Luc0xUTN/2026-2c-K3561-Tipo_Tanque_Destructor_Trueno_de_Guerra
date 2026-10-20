using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TGC.MonoGame.TP.Cameras;
using TGC.MonoGame.TP.Views;

namespace TGC.MonoGame.TP.scenes;

public class MenuScene : Scene
{

    private Battlefield _battlefield;
    
    private BaseCamera _camera;
    
    private GraphicsDevice _device;
    private ContentManager _contentManager;
    private Game _game; 
    
    private Matrix _projection;
    
    private SpriteFont _menuFont;

    private View _view;

    private SpriteBatch _spriteBatch;
    
    private bool _flTransition = false;
    
    public bool FlTransition { get => _flTransition; set =>  _flTransition = value ;}

    // Flanco de subida para "cualquier tecla" -- sin esto, si ya había una
    // tecla apretada al entrar a este Update (ej. volviendo desde GameScene
    // con una tecla de movimiento todavía presionada), GetPressedKeyCount()
    // disparaba la transición en el primer frame, sin que el jugador la
    // haya apretado realmente en el menú.
    private KeyboardState _previousKeyboardState;
    
    public MenuScene(Game game)
    {
        _game = game; 
    }


    
    public void Initialize()
    {
        _contentManager = new ContentManager(_game.Services, "Content");
        _device = _game.Services.GetService<IGraphicsDeviceService>().GraphicsDevice;

        Vector3 camPosition = new Vector3(150, 200, 150);
        
        _camera = new IsometricCamera(camPosition, Vector3.Zero, Vector3.Up, 0.15f);
        
        _battlefield = new Battlefield(ContentPaths.ContentFolderEffects + "BasicShader", Color.Green);
        _battlefield.Initialize(_device, GameSettings.MapSize); 
        
        int farPlaneDistance = (int) Math.Ceiling(Math.Sqrt(Math.Pow(GameSettings.MapSize, 2) + Math.Pow(GameSettings.MapSize, 2))); 
        _projection =
            Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, _device.Viewport.AspectRatio, 1, farPlaneDistance);
        
        _game.IsMouseVisible = true;

        _spriteBatch = new SpriteBatch(_device);

        // Se toma el estado del teclado en este momento como "ya apretada" --
        // así una tecla que venía sostenida desde antes de entrar al menú no
        // cuenta como una tecla nueva en el primer Update.
        _previousKeyboardState = Keyboard.GetState();
    }

    public void LoadContent()
    {
        _battlefield.LoadContent(_game.Content, ContentPaths.ContentFolder3D); // Lo cargo al contenido global debido a que no se debería destruir en ningún momento
        
        _menuFont = _contentManager.Load<SpriteFont>(ContentPaths.ContentFolderSpriteFonts + "Menu");
        _view = new MenuView(_device, this, _menuFont, _spriteBatch, _game.Exit);
        _view.Initialize();
    }

    public void Update(float elapsedTime)
    {
        KeyboardState keyboardState = Keyboard.GetState();
        MouseState mouseState = Mouse.GetState();

        // Transiciona solo con una tecla REALMENTE nueva (que no estuviera
        // apretada el frame anterior) -- ver comentario de _previousKeyboardState.
        foreach (var key in keyboardState.GetPressedKeys())
        {
            if (!_previousKeyboardState.IsKeyDown(key))
            {
                _flTransition = true;
                break;
            }
        }
        _previousKeyboardState = keyboardState;

        _view.Update();
        _camera.Update(elapsedTime);
    }

    public void Draw()
    {
        _battlefield.Draw(_device, _camera.GetView(),  _projection);
        
        _view.Draw();
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

    public EventHandler GetSetView(View view)
    {
        return (object sender, EventArgs args) =>
        {
            _view = view;
        }; 
    }


}