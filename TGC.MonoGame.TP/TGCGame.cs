using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Diagnostics;
using TGC.MonoGame.TP.Collisions;
using TGC.MonoGame.TP.Cameras;
using TGC.MonoGame.TP.Tanks.TurretCanonMovementHandlers;
using System.IO;

namespace TGC.MonoGame.TP;

/// <summary>
///     Esta es la clase principal del juego.
///     Inicialmente puede ser renombrado o copiado para hacer mas ejemplos chicos, en el caso de copiar para que se
///     ejecute el nuevo ejemplo deben cambiar la clase que ejecuta Program <see cref="Program.Main()" /> linea 10.
/// </summary>
public class TGCGame : Game
{
    public const string ContentFolder3D = "Models/";
    public const string ContentFolderEffects = "Effects/";
    public const string ContentFolderMusic = "Music/";
    public const string ContentFolderSounds = "Sounds/";
    public const string ContentFolderSpriteFonts = "SpriteFonts/";
    public const string ContentFolderTextures = "Textures/";
    
    private static readonly bool DebugIsometricCamera = false;

    private readonly GraphicsDeviceManager _graphics;
    private PhysicsSystem _physicsSystem;

    private bool _drawColliders = false;
    
    private Matrix _projection;

    private Battlefield _battlefield;

    private int _centerX;
    private int _centerY;
    
    private float _sensitivity = 0.005f;

    private readonly Keys _keyFreeLook = Keys.C;

    private Tank _tank;
    private Tank _enemyTank;

    private BaseCamera _camera;

    private TurretCanonMovementHandler _turretCanonMovementHandler;
    
    /// <summary>
    ///     Constructor del juego.
    /// </summary>
    public TGCGame()
    {
        // Maneja la configuracion y la administracion del dispositivo grafico.
        _graphics = new GraphicsDeviceManager(this);

        _graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width - 100;
        _graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height - 100;
        
        // Para que el juego sea pantalla completa se puede usar Graphics IsFullScreen.
        // Carpeta raiz donde va a estar toda la Media.
        Content.RootDirectory = "Content";
        // Hace que el mouse sea visible.
        IsMouseVisible = false;

    }

    /// <summary>
    ///     Se llama una sola vez, al principio cuando se ejecuta el ejemplo.
    ///     Escribir aqui el codigo de inicializacion: el procesamiento que podemos pre calcular para nuestro juego.
    /// </summary>
    protected override void Initialize()
    {
        if (_drawColliders)
            _physicsSystem = new DebugPhysicsSystem(GraphicsDevice);
        else
            _physicsSystem = new PhysicsSystem();

        float mapSize = 300f;
        _centerX = GraphicsDevice.Viewport.Width / 2;
        _centerY = GraphicsDevice.Viewport.Height / 2;
        
        _battlefield = new Battlefield(ContentFolderEffects + "BasicShader", Color.Green);
        _battlefield.Initialize(GraphicsDevice, _physicsSystem, mapSize);

        // El jugador siempre spawnea en el spawn A, sobre la altura real del
        // terreno en ese punto (Battlefield.GetHeightAt).
        var spawnPosition = _battlefield.GetSpawnA();
        var spawnHeight = _battlefield.GetHeightAt(spawnPosition.X, spawnPosition.Y);

        // La torreta y el cañón se apuntan con el mouse, que es lo que mueve la cámara.
        _turretCanonMovementHandler = new TurretCanonCameraAimHandler();
        _tank = new PlayerTank(new Vector3(spawnPosition.X, spawnHeight, spawnPosition.Y), Vector3.One, new Vector3(0,0,0), Color.Red);
        _tank.Initialize();

        _enemyTank = new Tank(new Vector3(1,spawnHeight,1), Vector3.One, new  Vector3(0,0,0), Color.Red);
        _enemyTank.Initialize();

        // Apago el backface culling.
        // Esto se hace por un problema en el diseno del modelo del logo de la materia.
        // Una vez que empiecen su juego, esto no es mas necesario y lo pueden sacar.
        var rasterizerState = new RasterizerState();
        rasterizerState.CullMode = CullMode.None;
        GraphicsDevice.RasterizerState = rasterizerState;
        // Seria hasta aca.

        // Configuramos nuestras matrices de la escena.

        _camera = DebugIsometricCamera
            ? new IsometricCamera(new Vector3(200, 220, 200), Vector3.Zero, Vector3.Up)
            : new OrbitalCamera(_tank, 20, _centerX, _centerY, _sensitivity);
        _camera.Initialize();
        
        _projection =
            Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, GraphicsDevice.Viewport.AspectRatio, 1, (int) Math.Ceiling(Math.Sqrt(Math.Pow(mapSize, 2)  + Math.Pow(mapSize, 2))));

        Mouse.SetPosition(_centerX,_centerY);

        _physicsSystem.Add(_tank);
        _physicsSystem.Add(_enemyTank);

        base.Initialize();
    }

    /// <summary>
    ///     Se llama una sola vez, al principio cuando se ejecuta el ejemplo, despues de Initialize.
    ///     Escribir aqui el codigo de inicializacion: cargar modelos, texturas, estructuras de optimizacion, el procesamiento
    ///     que podemos pre calcular para nuestro juego.
    /// </summary>
    protected override void LoadContent()
    {
        _battlefield.LoadContent(Content, ContentFolder3D);
        Model panzer = Content.Load<Model>(ContentFolder3D + "tanks/Panzer/Panzer");
        Model t90 = Content.Load<Model>( ContentFolder3D + "tanks/T90/T90");
        Effect effect = Content.Load<Effect>(ContentFolderEffects + "BasicShader");

        _tank.LoadContent(panzer, effect, "Turret", "Cannon");
        _enemyTank.LoadContent(panzer, effect, "Turret", "Cannon");
        
        base.LoadContent();
    }

    /// <summary>
    ///     Se llama en cada frame.
    ///     Se debe escribir toda la logica de computo del modelo, asi como tambien verificar entradas del usuario y reacciones
    ///     ante ellas.
    /// </summary>
    protected override void Update(GameTime gameTime)
    {
        
        // Capturar Input teclado
        if (Keyboard.GetState().IsKeyDown(Keys.Escape))
        {
            //Salgo del juego.
            Exit();
        }
        
        var elapsedTime = Convert.ToSingle(gameTime.ElapsedGameTime.TotalSeconds);

        KeyboardState keyboardState = Keyboard.GetState();
        MouseState mouseState = Mouse.GetState();

        // Mantener esta tecla equivale al Free Look de War Thunder: el mouse mueve la cámara
        // y la torreta y el cañón se quedan clavados en el último punto apuntado.
        bool freeLook = keyboardState.IsKeyDown(_keyFreeLook);

        // La cámara es la única que consume el mouse.
        _camera.IsFreeLook = freeLook;
        _camera.Update(keyboardState, mouseState, elapsedTime);

        // La torreta y el cañón siguen a la cámara, salvo durante el free look, en cuyo caso quedan
        // donde estaban y slewan hasta la nueva puntería en el momento de soltarla.
        _turretCanonMovementHandler.Update(
            _tank.GetTurret(),
            _tank.GetCanon(),
            _camera.Yaw - _tank.GetHullYaw(),
            _camera.Pitch,
            freeLook
        );

        _tank.Update(elapsedTime);
        _enemyTank.Update(elapsedTime);
        
        Mouse.SetPosition(_centerX,_centerY);

        _physicsSystem.Update();

        base.Update(gameTime);
    }

    /// <summary>
    ///     Se llama cada vez que hay que refrescar la pantalla.
    ///     Escribir aqui el codigo referido al renderizado.
    /// </summary>
    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);
        // GraphicsDevice.DepthStencilState = DepthStencilState.Default;
        
        var view = _camera.GetView();
        
        _battlefield.Draw(GraphicsDevice, view, _projection);
        _tank.Draw(GraphicsDevice, view, _projection);
        _enemyTank.Draw(GraphicsDevice, view, _projection);

        if (_physicsSystem is DebugPhysicsSystem)
            ((DebugPhysicsSystem) _physicsSystem).Draw(view, _projection);

        DebugManager.Draw(GraphicsDevice, view, _projection);
    }

    /// <summary>
    ///     Libero los recursos que se cargaron en el juego.
    /// </summary>
    protected override void UnloadContent()
    {
        // Libero los recursos.
        Content.Unload();

        base.UnloadContent();
    }
}