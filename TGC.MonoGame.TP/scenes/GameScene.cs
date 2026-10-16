using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TGC.MonoGame.TP.Cameras;
using TGC.MonoGame.TP.Collisions;
using TGC.MonoGame.TP.Tanks.TurretCanonMovementHandlers;

namespace TGC.MonoGame.TP.scenes;

public class GameScene : Scene
{
    private const float MapSize = 300f;
    private float _sensitivity = 0.005f;
    
    private bool _drawColliders = false;
    private static readonly bool DebugIsometricCamera = false;
    
    private PhysicsSystem _physicsSystem;

    private int _centerX;
    private int _centerY; 
    
    private BaseCamera _camera;
    
    private Battlefield _battlefield = null;
    
    
    // TODO: Esto debe ser pasado a la clase tanque debido a que el tanque debe saber como se mueve su torreta y cañon
    private TurretCanonCameraAimHandler _turretCanonMovementHandler; 
    private Tank _tank; 
    private List<EnemyTank> _enemyTanks;
    
    private Matrix _projection;

    private readonly Keys _keyFreeLook = Keys.C;
    
    // Recursos de la mira (retículo) que se dibuja sobre la escena.
    private SpriteBatch _spriteBatch;
    private Texture2D _reticlePixel;

    private Game _game; 
    private GraphicsDevice _device;
    private ContentManager _contentManager;    
    
    
    public GameScene(Game game)
    {
        _game = game;
    }
    
    public void Initialize()
    {
        // La creación de un content manager propio se encuentra relacionado con el hecho de que, cuando realicemos cambios de escena
        // Se van a tener que eliminar los recursos que no se utilizan más de la GPU, entonces, para lograr esto se crea un content
        // manager propio
        _contentManager = new ContentManager(_game.Services, "Content");

        _device = _game.Services.GetService<IGraphicsDeviceService>().GraphicsDevice;
        if (_drawColliders)
            _physicsSystem = new DebugPhysicsSystem(_device);
        else
            _physicsSystem = new PhysicsSystem();
        
        _centerX = _device.Viewport.Width / 2;
        _centerY = _device.Viewport.Height / 2;

        if (_battlefield == null)
        {
            _battlefield = new Battlefield(ContentPaths.ContentFolderEffects + "BasicShader", Color.Green);
            _battlefield.Initialize(_device, _physicsSystem, MapSize);
        }
        
        // El jugador siempre spawnea en el spawn A, sobre la altura real del
        // terreno en ese punto (Battlefield.GetHeightAt).
        var spawnPosition = _battlefield.GetSpawnA();
        var spawnHeight = _battlefield.GetHeightAt(spawnPosition.X, spawnPosition.Y);
        
        // La torreta y el cañón se apuntan con el mouse, que es lo que mueve la cámara.
        _turretCanonMovementHandler = new TurretCanonCameraAimHandler();
        _tank = new PlayerTank(new Vector3(spawnPosition.X, spawnHeight, spawnPosition.Y), Vector3.One, new Vector3(0,0,0), Color.Red, _battlefield);
        _tank.Initialize();

        // Enemigos: spawnean cerca del spawn B (el opuesto al jugador), separados
        // entre sí para no superponerse. Radio de separación estimado a ojo contra
        // el ancho del claro de spawn (22u) y el tamaño del tanque.
        var enemySpawnCenter = _battlefield.GetSpawnB();
        float[] enemyOffsetsX = { -10f, 0f, 10f };
        _enemyTanks = new List<EnemyTank>();
        foreach (var offsetX in enemyOffsetsX)
        {
            var enemyX = enemySpawnCenter.X + offsetX;
            var enemyZ = enemySpawnCenter.Y;
            var enemyHeight = _battlefield.GetHeightAt(enemyX, enemyZ);

            var enemyAI = new AI(_tank, _battlefield);
            var enemyTank = new EnemyTank(new Vector3(enemyX, enemyHeight, enemyZ), Vector3.One, new Vector3(0, 0, 0), Color.Blue, enemyAI, _battlefield);
            enemyTank.Initialize();
            _enemyTanks.Add(enemyTank);
        }
        
        _camera = DebugIsometricCamera
            ? new IsometricCamera(new Vector3(200, 220, 200), Vector3.Zero, Vector3.Up)
            : new OrbitalCamera(_tank, 20, _centerX, _centerY, _sensitivity, _battlefield.GetHeightAt);
        _camera.Initialize();
        
        _projection =
            Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, _device.Viewport.AspectRatio, 1, (int) Math.Ceiling(Math.Sqrt(Math.Pow(MapSize, 2)  + Math.Pow(MapSize, 2))));
        
        Mouse.SetPosition(_centerX,_centerY);

        _physicsSystem.Add(_tank);
        _enemyTanks.ForEach(t => _physicsSystem.Add(t));
    }

    public void LoadContent()
    {
        _battlefield.LoadContent(_contentManager, ContentPaths.ContentFolder3D);

        Effect effect = _contentManager.Load<Effect>(ContentPaths.ContentFolderEffects + "BasicShader");

        // Cada tanque carga el modelo con su propio ContentManager. Si todos
        // comparten uno (como antes), ContentManager cachea el Model por path y
        // los 4 tanques terminan compartiendo los mismos ModelBone de torreta y
        // cañón -- el último que actualiza en el frame pisa a los demás (ver
        // progress/09 en el repo de contexto).
        Model playerModel = new ContentManager(_game.Services, _contentManager.RootDirectory).Load<Model>(ContentPaths.ContentFolder3D + "tanks/Panzer/Panzer");
        _tank.LoadContent(playerModel, effect, "Turret", "Cannon");

        foreach (var enemyTank in _enemyTanks)
        {
            Model enemyModel = new ContentManager(_game.Services, _contentManager.RootDirectory).Load<Model>(ContentPaths.ContentFolder3D + "tanks/Panzer/Panzer");
            enemyTank.LoadContent(enemyModel, effect, "Turret", "Cannon");
        }

        // Un único pixel blanco reescalado alcanza para armar la mira del HUD.
        _spriteBatch = new SpriteBatch(_device);
        _reticlePixel = new Texture2D(_device, 1, 1);
        _reticlePixel.SetData(new[] { Color.White });
    }

    public void Update(float elapsedTime)
    {
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
        
        foreach (var enemyTank in _enemyTanks)
        {
            enemyTank.Update(elapsedTime);
        }

        Mouse.SetPosition(_centerX,_centerY);
        
        _physicsSystem.Update();
    }

    public void Draw()
    {
        var view = _camera.GetView();
        
        _battlefield.Draw(_device, view, _projection);
        _tank.Draw(_device, view, _projection);
        
        foreach (var enemyTank in _enemyTanks)
        {
            enemyTank.Draw(_device, _camera.GetView(), _projection);
        }

        if (_physicsSystem is DebugPhysicsSystem)
            ((DebugPhysicsSystem) _physicsSystem).Draw(view, _projection);

        DebugManager.Draw(_device, view, _projection);

        _spriteBatch.Begin();
         DrawReticle(view, _device);
        _spriteBatch.End();
    }   
    
    // TODO: Este método junto a todo lo que tiene que ver con el dibujo del reticulo tiene que ser abstraido a una clase debido 
    // TODO: A que no es responsabilidad del TGCGame y ahora tampoco de GameScene implementar la lógica de dibuado de un reticulo
    // Mira : la cruz se proyecta desde la dirección que marca el mouse, así que
    // sigue al mouse en pantalla y la cámara la recentra a medida que se acomoda detrás del tanque.
    // La torreta y el cañón persiguen esa misma dirección, más lento todavía.
    private void DrawReticle(Matrix view, GraphicsDevice device)
    {
        const int gap = 5;
        const int arm = 14;
        const int thickness = 2;
        int half = thickness / 2;
        Color color = new Color(0.25f, 1f, 0.25f, 0.9f);

        // Punto lejano en la dirección de puntería, saliendo de la torreta.
        Vector3 aimPoint = _tank.GetPosition() + Vector3.Up * 2.5f + _camera.AimDirection * 200f;

        Vector3 screen = device.Viewport.Project(aimPoint, _projection, view, Matrix.Identity);
        if (screen.Z < 0f || screen.Z > 1f)
            return; // quedó detrás de la cámara

        // Se mantiene siempre dentro de la pantalla, aun apuntando al límite del rango del cañón.
        int margin = gap + arm;
        int cx = (int)MathHelper.Clamp(screen.X, margin, device.Viewport.Width - margin);
        int cy = (int)MathHelper.Clamp(screen.Y, margin, device.Viewport.Height - margin);

        // Brazos horizontales.
        _spriteBatch.Draw(_reticlePixel, new Rectangle(cx - gap - arm, cy - half, arm, thickness), color);
        _spriteBatch.Draw(_reticlePixel, new Rectangle(cx + gap, cy - half, arm, thickness), color);
        // Brazos verticales.
        _spriteBatch.Draw(_reticlePixel, new Rectangle(cx - half, cy - gap - arm, thickness, arm), color);
        _spriteBatch.Draw(_reticlePixel, new Rectangle(cx - half, cy + gap, thickness, arm), color);
        // Punto central.
        _spriteBatch.Draw(_reticlePixel, new Rectangle(cx - 1, cy - 1, 2, 2), color);
    }

    public void Unload()
    {
        _contentManager.Unload();   
    }

    public bool ShouldTransition()
    {
        // TODO: Implementar la lógica de ganar o perder
        return false; 
    }

    public Scene Transition()
    {
        this.Unload();
        return null; 
    }
}