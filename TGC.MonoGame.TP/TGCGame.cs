using System;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
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
    
    private readonly GraphicsDeviceManager _graphics;
    
    private Matrix _projection;

    private Terrain _terrain;
    private Forest _forest;
    
    private List<Prop> _tanksAllies = new List<Prop>();
    private List<Prop> _tanksEnemies = new List<Prop>(); 
    
    private Camera _camera;
    
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

        int maxTanks = 5;
        
        Vector3 enemyPosition = new Vector3(0, 1, 100);
        Vector3 alliesPosition = new Vector3(0, 1, -100);
        Vector3 tanksOffset = new Vector3(10,0,0);
        Vector3 alliesScale = new Vector3(0.01f, 0.01f, 0.01f);
        Vector3 enemyScale = new Vector3(1f, 1f, 1f);
 
        Vector3 tankRotation = Vector3.Zero;
        Vector3 enemyRotation = new Vector3(0, - (float) Math.PI/2, 0);
        
        for (int i = 0; i < maxTanks; ++i)
        {
            Prop allieTank = new Prop(alliesPosition, alliesScale, Vector3.Zero, Color.Gold);
            allieTank.Initialize();

            
            Prop enemyTank = new Prop(enemyPosition, enemyScale, enemyRotation, Color.Red); 
            enemyTank.Initialize();
            
            _tanksAllies.Add(allieTank);
            _tanksEnemies.Add(enemyTank); 
            
            alliesPosition += tanksOffset;
            enemyPosition += tanksOffset;
        }
        
        _terrain = new Terrain(ContentFolderEffects + "BasicShader", Color.Green);
        _terrain.Initialize(GraphicsDevice, mapSize);

        _forest = new Forest();
        _forest.Initialize(new Vector2(mapSize, mapSize));
        
        // Apago el backface culling.
        // Esto se hace por un problema en el diseno del modelo del logo de la materia.
        // Una vez que empiecen su juego, esto no es mas necesario y lo pueden sacar.
        var rasterizerState = new RasterizerState();
        rasterizerState.CullMode = CullMode.None;
        GraphicsDevice.RasterizerState = rasterizerState;
        // Seria hasta aca.

        // Configuramos nuestras matrices de la escena.
        // Esta hecho con el objetivo de que observe al terreno de forma isometrica 
        
        _camera = new Camera(new Vector3(100,100,100), Vector3.Zero, Vector3.Up);
        _camera.Initialize();
        
        _projection =
            Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, GraphicsDevice.Viewport.AspectRatio, 1, (int) Math.Ceiling(Math.Sqrt(Math.Pow(mapSize, 2)  + Math.Pow(mapSize, 2))));
        
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
        
        _tanksAllies.ForEach(t => t.LoadContent(Content, ContentFolder3D + "tanks/Panzer/Panzer",  ContentFolderEffects + "BasicShader"));
        _tanksEnemies.ForEach(t => t.LoadContent(Content, ContentFolder3D + "tanks/T90/T90",  ContentFolderEffects + "BasicShader"));
        
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
        
       _camera.Update(Keyboard.GetState(), elapsedTime);
       
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
        
        _terrain.Draw(GraphicsDevice, _camera.GetView(), _projection);
        _forest.Draw(GraphicsDevice, _camera.GetView(), _projection);
        
        _tanksEnemies.ForEach(t => t.Draw(GraphicsDevice, _camera.GetView(), _projection));
        _tanksAllies.ForEach(t => t.Draw(GraphicsDevice, _camera.GetView(), _projection));
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