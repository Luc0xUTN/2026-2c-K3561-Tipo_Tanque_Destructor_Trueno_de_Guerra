using System;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TGC.MonoGame.TP.Cameras;
using TGC.MonoGame.TP.Tanks.TurretCanonMovementHandlers;
using Vector2 = Microsoft.Xna.Framework.Vector2;
using Vector3 = Microsoft.Xna.Framework.Vector3;

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
    
    private Matrix _projection;

    private Battlefield _battlefield;

    private int _centerX;
    private int _centerY;
    
    private float _sensitivity = 0.005f;

    private Tank _tank;
    
    private BaseCamera _camera;
    
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
        float mapSize = 300f;
        _centerX = GraphicsDevice.Viewport.Width / 2;
        _centerY = GraphicsDevice.Viewport.Height / 2;
        
        
        _battlefield = new Battlefield(ContentFolderEffects + "BasicShader", Color.Green);
        _battlefield.Initialize(GraphicsDevice, mapSize);

        // El jugador siempre spawnea en el spawn A, sobre la altura real del
        // terreno en ese punto (Battlefield.GetHeightAt).
        var spawnPosition = _battlefield.GetSpawnA();
        var spawnHeight = _battlefield.GetHeightAt(spawnPosition.X, spawnPosition.Y);

        TurretCanonMovementHandler turretCanonMovementHandler = new TurretCanonKeyboardHandler(); 
        _tank = new PlayerTank(new Vector3(spawnPosition.X, spawnHeight, spawnPosition.Y), new  Vector3(0.01f, 0.01f,0.01f), new  Vector3(0,0,0), Color.Red, turretCanonMovementHandler);
        _tank.Initialize();

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
            : new OrbitalCamera(_tank, 20, 0, 0, _centerX, _centerY, _sensitivity);
        _camera.Initialize();
        
        _projection =
            Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, GraphicsDevice.Viewport.AspectRatio, 1, (int) Math.Ceiling(Math.Sqrt(Math.Pow(mapSize, 2)  + Math.Pow(mapSize, 2))));
        

        Mouse.SetPosition(_centerX,_centerY);
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
        
        base.LoadContent();
    }

    /// <summary>
    ///     Se llama en cada frame.
    ///     Se debe escribir toda la logica de computo del modelo, asi como tambien verificar entradas del usuario y reacciones
    ///     ante ellas.
    /// </summary>
    protected override void Update(GameTime gameTime)
    {
        // Aca deberiamos poner toda la logica de actualizacion del juego.
        
        // Capturar Input teclado
        if (Keyboard.GetState().IsKeyDown(Keys.Escape))
        {
            //Salgo del juego.
            Exit();
        }
        
        var elapsedTime = Convert.ToSingle(gameTime.ElapsedGameTime.TotalSeconds);
        
       
       _tank.Update(elapsedTime);
       
       _camera.Update(Keyboard.GetState(), Mouse.GetState(),  elapsedTime);
       
       Mouse.SetPosition(_centerX,_centerY);
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
        
        _battlefield.Draw(GraphicsDevice, _camera.GetView(), _projection);
        _tank.Draw(GraphicsDevice, _camera.GetView(), _projection);
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