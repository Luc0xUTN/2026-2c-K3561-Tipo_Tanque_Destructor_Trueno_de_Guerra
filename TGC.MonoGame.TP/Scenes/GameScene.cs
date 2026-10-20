using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TGC.MonoGame.TP.Cameras;
using TGC.MonoGame.TP.Collisions;
using TGC.MonoGame.TP.Tanks.TurretCanonMovementHandlers;
using TGC.MonoGame.TP.Views;

namespace TGC.MonoGame.TP.scenes;

public class GameScene : Scene
{

    
    private bool _drawColliders = false;
    
    private PhysicsSystem _physicsSystem;

    private int _centerX;
    private int _centerY; 
    
    private BaseCamera _camera;
    
    private Battlefield _battlefield = null;
    
    
    // TODO: Esto debe ser pasado a la clase tanque debido a que el tanque debe saber como se mueve su torreta y cañon
    private TurretCanonCameraAimHandler _turretCanonMovementHandler; 
    private Tank _tank;
    private List<EnemyTank> _enemyTanks;
    private List<Projectile> _projectiles;

    private Matrix _projection;

    private readonly Keys _keyFreeLook = Keys.C;

    // Recursos de la mira (retículo) que se dibuja sobre la escena.
    private SpriteBatch _spriteBatch;
    private Texture2D _reticlePixel;

    // HUD (vida del jugador, contador de enemigos) -- mismo SpriteBatch/pixel
    // que la mira, se dibuja en el mismo Begin()/End().
    private SpriteFont _hudFont;
    private GameHudView _hudView;

    private Game _game;
    private GraphicsDevice _device;
    private ContentManager _contentManager;

    // Guardados desde LoadContent para poder instanciar proyectiles sobre la
    // marcha cuando un tanque dispara (Update corre muchas veces, LoadContent
    // una sola). Modelo placeholder (Rock0) -- no hay asset de proyectil propio
    // todavía, "no somos diseñadores" así que se reusa uno existente a escala chica.
    private Effect _effect;
    private Model _projectileModel;
    
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
            _battlefield.Initialize(_device, GameSettings.MapSize);
        }
        _battlefield.AddEntitiesToPhyisicSystem(_physicsSystem);
        
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

        _projectiles = new List<Projectile>();

        _camera = GameSettings.DebugMode
            ? new IsometricCamera(new Vector3(200, 220, 200), Vector3.Zero, Vector3.Up)
            : new OrbitalCamera(_tank, 20, _centerX, _centerY, GameSettings.Sensitivity, _battlefield.GetHeightAt);
        _camera.Initialize();


        int farPlaneDistance = (int) Math.Ceiling(Math.Sqrt(Math.Pow(GameSettings.MapSize, 2) + Math.Pow(GameSettings.MapSize, 2))); 
        _projection =
            Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, _device.Viewport.AspectRatio, 1, farPlaneDistance);
        
        Mouse.SetPosition(_centerX,_centerY);

        _physicsSystem.Add(_tank);
        _enemyTanks.ForEach(t => _physicsSystem.Add(t));
        
        _game.IsMouseVisible = false; 
    }

    public void LoadContent()
    {
        if (!_battlefield.FlContentLoaded)
        {
            _battlefield.LoadContent(_contentManager, ContentPaths.ContentFolder3D);
        }

        _effect = _contentManager.Load<Effect>(ContentPaths.ContentFolderEffects + "BasicShader");

        // Cada tanque carga el modelo con su propio ContentManager. Si todos
        // comparten uno (como antes), ContentManager cachea el Model por path y
        // los 4 tanques terminan compartiendo los mismos ModelBone de torreta y
        // cañón -- el último que actualiza en el frame pisa a los demás.
        Model playerModel = new ContentManager(_game.Services, _contentManager.RootDirectory).Load<Model>(ContentPaths.ContentFolder3D + "tanks/Panzer/Panzer");
        _tank.LoadContent(playerModel, _effect, "Turret", "Cannon");

        foreach (var enemyTank in _enemyTanks)
        {
            Model enemyModel = new ContentManager(_game.Services, _contentManager.RootDirectory).Load<Model>(ContentPaths.ContentFolder3D + "tanks/Panzer/Panzer");
            enemyTank.LoadContent(enemyModel, _effect, "Turret", "Cannon");
        }

        _projectileModel = _contentManager.Load<Model>(ContentPaths.ContentFolder3D + "projectiles/Bullet");

        // Un único pixel blanco reescalado alcanza para armar la mira del HUD.
        _spriteBatch = new SpriteBatch(_device);
        _reticlePixel = new Texture2D(_device, 1, 1);
        _reticlePixel.SetData(new[] { Color.White });

        // Reusa la misma fuente del menú -- no hay una fuente propia del HUD todavía.
        _hudFont = _contentManager.Load<SpriteFont>(ContentPaths.ContentFolderSpriteFonts + "Menu");
        _hudView = new GameHudView(_device, _hudFont, _spriteBatch, _reticlePixel, _tank, _enemyTanks, _camera, _projection);
        _hudView.Initialize();
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
        _camera.Update(elapsedTime);

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

        TrySpawnProjectile(_tank);
        foreach (var enemyTank in _enemyTanks)
        {
            TrySpawnProjectile(enemyTank);
        }

        var allTanks = new List<Tank> { _tank };
        allTanks.AddRange(_enemyTanks);
        foreach (var projectile in _projectiles)
        {
            projectile.Update(elapsedTime, allTanks, _physicsSystem.StaticCollidables, _battlefield.GetHeightAt);

            foreach (var destroyedProp in projectile.DestroyedProps)
            {
                _physicsSystem.Remove(destroyedProp);
                _battlefield.RemoveProp(destroyedProp);
            }
        }
        _projectiles.RemoveAll(p => !p.IsAlive);

        // Tanques enemigos muertos: se sacan de colisiones y de la lista que
        // actualiza/dibuja GameScene. La condición de victoria (ShouldTransition)
        // se basa en que no quede ninguno.
        foreach (var deadEnemy in _enemyTanks.FindAll(e => e.IsDead))
        {
            _physicsSystem.Remove(deadEnemy);
        }
        _enemyTanks.RemoveAll(e => e.IsDead);

        _hudView.Update();

        Mouse.SetPosition(_centerX,_centerY);

        _physicsSystem.Update();
    }

    // La posición/dirección de spawn salen del transform absoluto del bone del
    // cañón (casco -> torreta -> cañón, multiplicado y recién ahí llevado al
    // mundo) -- no alcanza con un offset fijo desde la posición del tanque,
    // porque la torreta apunta independiente de hacia dónde mira el casco.
    private void TrySpawnProjectile(Tank tank)
    {
        if (!tank.WantsToFire) return;

        const float muzzleSpeed = 60f; // provisorio, a ajustar por feel.

        var canonTransform = tank.GetCanonWorldTransform();
        var spawnPosition = canonTransform.Translation;
        // Negado: mismo problema de convención que Move(-1) en PlayerTank -- el
        // modelo tiene el eje "Forward" invertido respecto de hacia dónde apunta
        // visualmente el cañón (verificado con el log de spawn: con el signo
        // original, los disparos salían hacia atrás del tanque).
        var direction = -canonTransform.Forward;
        direction.Normalize();

        var projectile = new Projectile(spawnPosition, direction * muzzleSpeed, tank);
        projectile.Initialize();
        projectile.LoadContent(_projectileModel, _effect);
        _projectiles.Add(projectile);
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

        foreach (var projectile in _projectiles)
        {
            projectile.Draw(_device, view, _projection);
        }

        if (_physicsSystem is DebugPhysicsSystem)
            ((DebugPhysicsSystem) _physicsSystem).Draw(view, _projection);

        DebugManager.Draw(_device, view, _projection);

        _hudView.Draw();
    }
    
    // TODO: Este método junto a todo lo que tiene que ver con el dibujo del reticulo tiene que ser abstraido a una clase debido 
    // TODO: A que no es responsabilidad del TGCGame y ahora tampoco de GameScene implementar la lógica de dibuado de un reticulo

    public void SetBattlefield(Battlefield battlefield)
    {
        this._battlefield = battlefield;
    }
    
    public void Unload()
    {
        _contentManager.Unload();   
    }

    public bool ShouldTransition()
    {
        return _tank.IsDead || _enemyTanks.Count == 0;
    }

    public Scene Transition()
    {
        // Versión rápida para validar la idea end-to-end: solo mensaje por
        // consola, sin pantalla de fin todavía (eso queda para una fase posterior).
        if (_tank.IsDead)
            Console.WriteLine("GAME OVER -- el jugador fue destruido.");
        else
            Console.WriteLine("VICTORIA -- todos los tanques enemigos fueron destruidos.");

        this.Unload();

        // TGCGame.Update llama Initialize()/LoadContent() sobre lo que devuelva
        // esto sin chequear null -- tiene que ser una escena real, no null
        // (mismo patrón que MenuScene.Transition al pasar a GameScene).
        return new MenuScene(_game);
    }
}