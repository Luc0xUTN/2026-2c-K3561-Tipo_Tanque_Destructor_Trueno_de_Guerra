using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using TGC.MonoGame.TP.Collisions;

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
    
    private readonly GraphicsDeviceManager _graphics;

    private Effect _effect;
    private Model _model;
    private Matrix _projection;
    private float _rotation;
    private SpriteBatch _spriteBatch;
    private Matrix _view;
    private Matrix _world;

    private Terrain _terrain;
    private Forest _forest;

    private int _centerX;
    private int _centerY;
    

    private Tank _tank;
    private Tank _enemyTank;
    
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
        IsMouseVisible = true;
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
        
        
        // Y=10: por encima de cualquier altura que pueda generar el heightmap (rango
        // ±4.5, ver Terrain.HeightRange), para no spawnear enterrado en una colina.
        // Es un valor fijo temporal — cuando el tanque consulte Terrain.GetHeightAt
        // en su propio (x,z) esto debería reemplazarse por la altura real del terreno.
        _tank = new PlayerTank(new Vector3(1,10,1), Vector3.One, new  Vector3(0,0,0), Color.Red);
        _tank.Initialize();

        _enemyTank = new Tank(new Vector3(1,10,1), Vector3.One, new  Vector3(0,0,0), Color.Red);
        _enemyTank.Initialize();
        
        // TODO: seed temporal acá. Por diseño (ver progress/05-world-generation-pipeline.md
        // del repo de contexto) esto lo tiene que terminar decidiendo Battlefield, que
        // todavía es un stub vacío. Mientras tanto queda fija acá para poder generar el
        // terreno y tener un "mapa tipo" reproducible para debug.
        const int worldSeed = 12345;

        // Apago el backface culling.
        // Esto se hace por un problema en el diseno del modelo del logo de la materia.
        // Una vez que empiecen su juego, esto no es mas necesario y lo pueden sacar.
        var rasterizerState = new RasterizerState();
        rasterizerState.CullMode = CullMode.None;
        GraphicsDevice.RasterizerState = rasterizerState;
        // Seria hasta aca.

        // Configuramos nuestras matrices de la escena.
        _world = Matrix.Identity;
        _view = Matrix.CreateLookAt(Vector3.UnitZ * 150, Vector3.Zero, Vector3.Up);
        _projection =
            Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, GraphicsDevice.Viewport.AspectRatio, 1, 250);

        DebugManager.Add(
            new ColliderGizmo(GraphicsDevice, _tank.Collider, true)
        );
        DebugManager.Add(
            new ColliderGizmo(GraphicsDevice, _enemyTank.Collider, true)
        );

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
        _terrain.LoadContent(Content);
        _forest.LoadContent(Content, ContentFolder3D, ContentFolderEffects + "BasicShader");
        
        Model panzer = Content.Load<Model>(ContentFolder3D + "tanks/Panzer/Panzer");
        Model t90 = Content.Load<Model>( ContentFolder3D + "tanks/T90/T90");
        Effect effect = Content.Load<Effect>(ContentFolderEffects + "BasicShader");        
        
        _tank.LoadContent(panzer, effect);
        _enemyTank.LoadContent(panzer, effect);
        
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

       Debug.Print(CollisionDetector.CheckCollision(_tank.Collider, _enemyTank.Collider).ToString());
       base.Update(gameTime);
    }

    /// <summary>
    ///     Se llama cada vez que hay que refrescar la pantalla.
    ///     Escribir aqui el codigo referido al renderizado.
    /// </summary>
    protected override void Draw(GameTime gameTime)
    {
        // Aca deberiamos poner toda la logia de renderizado del juego.
        GraphicsDevice.Clear(Color.Black);
        // GraphicsDevice.DepthStencilState = DepthStencilState.Default;

        var view = _camera.GetView();
        
        _terrain.Draw(GraphicsDevice, view, _projection);
        _forest.Draw(GraphicsDevice, view, _projection);
        _tank.Draw(GraphicsDevice, view, _projection);
        _enemyTank.Draw(GraphicsDevice, view, _projection);

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