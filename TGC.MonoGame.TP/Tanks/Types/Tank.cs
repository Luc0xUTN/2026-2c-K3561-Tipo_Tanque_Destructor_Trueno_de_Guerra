using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.Cameras;
using TGC.MonoGame.TP.Collisions;
using TGC.MonoGame.TP.Tanks;
using Quaternion = Microsoft.Xna.Framework.Quaternion;
using Vector3 = Microsoft.Xna.Framework.Vector3;

namespace TGC.MonoGame.TP;

// Responsabilidad: Representar a un tanque en sí 
public class Tank : Entity
{ 
    private Texture _caterpillarTexture;

    private List<ModelMesh> _generalMeshes;
    private ModelMesh _leftCaterpillarMesh;
    private ModelMesh _rightCaterpillarMesh;
    private List<ModelBone> _leftWheelBones = new List<ModelBone>();
    private List<ModelBone> _rightWheelBones = new List<ModelBone>();
    private ModelBone _leftDriveSprocket;
    private ModelBone _rightDriveSprocket;
    
    private Texture _tankTexture;

    // Correcciones para modelos que no se modelaron con la misma convención
    // que el Panzer (hoy: el T90) -- ver LoadContent. 0 / false = sin
    // corrección, el comportamiento de siempre.
    private float _visualGroundOffset = 0f;

    // Corrección real del T90 (no un parche visual): Turret/Cannon se
    // modelaron con una rotación D de reposo distinta a la del Panzer. En vez
    // de REEMPLAZAR esa rotación (lo que rompía el reposo real, confirmado
    // jugando y con renders aislados fuera del juego), se las deja intactas
    // y se corrige girando alrededor del eje correcto: E = D * RotX(90°)
    // conjuga el yaw/pitch estándar (ejes X/Y, fijos en RotationalObject, no
    // tocable) al eje que de verdad corresponde en este modelo. Validado con
    // renders aparte (Panzer vs T90, reposo y en movimiento, antes de tocar
    // el juego) -- ver memoria del proyecto para el detalle de la derivación.
    private Matrix _t90RigCorrectionInverse = Matrix.Identity;

    // Cannon no está anidado bajo Turret en la jerarquía real del T90 (son
    // hermanos, los dos cuelgan de un nodo "Empty" neutro -- en el Panzer sí
    // está anidado). No se puede reparentar un ModelBone en tiempo de
    // ejecución, así que GetCannonAbsolute lo pega a mano a la posición ya
    // corregida de Turret en vez de usar _absoluteWorldsMatrix[_canon.BoneIndex]
    // directo.
    //
    // _cannonRelativeToTurret es el transform COMPLETO (rotación + traslación),
    // no solo un offset de traslación -- una primera versión (validada con
    // render pero mal leída: ver memoria del proyecto) usaba solo la
    // diferencia de traslaciones SIN rotarla por el reposo (D) de Turret, lo
    // que dejaba al caño apuntando para cualquier lado / "metido" en la
    // torreta en vez de saliendo derecho. El relativo completo, calculado
    // una sola vez acá con los bones CRUDOS (antes de aplicar ninguna
    // corrección), es la forma correcta de "pegar" un bone que no es
    // realmente hijo del otro.
    private bool _compensateCannonYaw = false;
    private Matrix _cannonRelativeToTurret = Matrix.Identity;
    private Matrix _cannonOriginalLocal = Matrix.Identity;
    private ModelMesh _cannonMesh;
    private ModelMesh _turretMesh;
    private ModelMesh _hullMesh;

    // Deformación por impacto de proyectil -- buffer circular en espacio
    // LOCAL del Hull (la instancia concreta de ESTE tanque, no el Model
    // compartido: cada tanque tiene su propio _effect/_impactPoints). Solo
    // la carrocería se abolla (decisión de Dante: es donde más probabilidad
    // hay de recibir un impacto) -- Turret/Cannon/ruedas no se tocan.
    // Radio/fuerza del empuje NO viven acá -- son constantes de compilación
    // en ModelShader.fx. Si hay que ajustarlas por feel, se tocan ahí.
    //
    // _hullLocalScale: el bone Hull trae pegada una escala (~100, igual en
    // Panzer y T90 -- confirmado leyendo la matriz cruda) que NO se puede
    // ignorar al sumar un offset en espacio local (ver ModelShader.fx). Se
    // calcula UNA SOLA VEZ acá, en C#, y se sube como uniform fijo -- A
    // PROPÓSITO no se lee la escala desde "World" dentro del shader: World
    // cambia de malla a malla en el mismo DrawMeshes (Cannon tiene su
    // propia escala no uniforme, ya documentada en GetCanonFiringDirection)
    // y, si el shader no saltea bien el cálculo para esas otras mallas
    // (el mismo compilador ya nos rompió un `if` antes con el indexado de
    // ImpactPoints), terminaba usando la escala del Cannon por error y el
    // caño desaparecía -- y eso NO alcanzó: el caño seguía sin verse
    // incluso en una partida sin ningún impacto todavía (ImpactCount=0 en
    // todos lados, matemáticamente un no-op). La causa real: el solo hecho
    // de que Turret/Cannon pasaran por un vertex shader con ESTE código de
    // más (aunque nunca se ejecutara su rama "activa") ya les rompía algo
    // en este compilador. Arreglado de raíz con una técnica de shader
    // APARTE (DeformedDrawing en ModelShader.fx) que solo se usa al
    // dibujar el Hull -- Turret/Cannon/ruedas pasan por MainVS, sin tocar,
    // byte a byte igual a como estaba antes de esta feature.
    private const int MaxImpactPoints = 8;
    private readonly Vector3[] _impactPoints = new Vector3[MaxImpactPoints];
    // Mismo contenido que _impactPoints, como Vector4 (w sin usar) -- es lo
    // que de verdad se sube al shader (ver el comentario de ImpactPoints en
    // ModelShader.fx: el compilador de SM3.0 no acepta indexar un array de
    // float3 con una variable de loop). Buffer reusado, no uno nuevo por frame.
    private readonly Vector4[] _impactPointsUpload = new Vector4[MaxImpactPoints];
    private int _impactCount;
    private int _nextImpactSlot;
    private float _hullLocalScale = 1f;
    // La velocidad terminal del modelo de orugas amortiguadas es ~accel/_dumping
    // (ver el comentario de Update más abajo) -- con _dumping=2 en el constructor,
    // 100 da ~50u/s, el doble que antes (era 50f ~= 25u/s). Afecta a todos los
    // tanques por igual (Tank es la base de PlayerTank y AITank).
    private float _acceleration = 50f;
    private float _deceleration = 3f; // no es frenado lineal -- controla qué tan brusco gira (diferencial entre orugas).


    private int _speedingDirection = 0;
    private int _rotationDirection = 0;

    private float _leftCaterpillarSpeed = 0;
    private float _leftCaterpillarPosition = 0.0f; 

    private float _rightCaterpillarSpeed = 0;
    private float _rightCaterpillarPosition = 0.0f;

    private float _dumping = 6.0f;
    
    private float _caterpillarDistance = 2.0f;
    private float _caterpillarTextureScale = 0.25f; 
    
    private float _wheelRadius = 0.5f;

    private float _yaw;

    // Velocidad lineal actual (escalar, a lo largo de _world.Forward) -- se
    // guarda como campo (antes era variable local de Update) para poder
    // exponerla vía GetVelocity() a CollisionResolver, que la necesita para
    // el clipping de velocidad en el choque.
    private float _tankSpeed;
    
    private Matrix[] _absoluteWorldsMatrix;
    
    private Battlefield _battlefield;
    
    protected Turret _turret;
    protected Canon _canon;

    // Seteado por PlayerTank/AITank cada Update -- GameScene lo lee después
    // de actualizar el tanque y, si está en true, crea el Projectile. Ni el
    // jugador ni la IA conocen a GameScene, solo "avisan" que quieren disparar
    // (mismo principio que TankIntention.ShouldFire).
    public bool WantsToFire { get; protected set; }

    // --- Vida ---
    private const int MaxHealth = 100;
    private const int ProjectileDamage = 10;
    // Cantidad "normal" de daño por choque físico (tanque-tanque, tanque-roca).
    // Pública porque Prop.TankCollisionDamage la usa como su propio default --
    // así una roca (que no pisa esa propiedad) da exactamente este daño, sin
    // duplicar el número en dos lugares. Tree la pisa por una menor (ver
    // Tree.cs) -- a propósito, para no castigar tanto abrirse camino por el
    // bosque en vez de ir por los senderos (a Dante le importa que SIGA
    // doliendo algo, para que no sea un atajo gratis, pero menos que chocar
    // contra algo sólido de verdad).
    public const int DefaultCollisionDamage = 2;
    // Sin el cooldown de abajo, un tanque trabado contra un obstáculo
    // perdería vida CADA FRAME mientras siga superpuesto -- se moriría en
    // menos de un segundo. Esto no limita el daño de un impacto de proyectil
    // (eso siempre aplica, ver TakeProjectileDamage).
    private const float CollisionDamageCooldownSeconds = 0.5f;
    private float _collisionDamageCooldownTimer;

    public int Health { get; private set; } = MaxHealth;
    public bool IsDead => Health <= 0;

    // Lo prende/apaga PlayerTank (God mode) -- genérico en Tank porque el
    // daño en sí (TakeProjectileDamage/TakeCollisionDamage) vive acá, no en
    // la subclase. No toca el choque físico (clipping de velocidad): God
    // mode anula el daño, no la física.
    public bool IsInvincible { get; set; }

    // Para el HUD: 0..1. El cast a float es necesario porque Health y
    // MaxHealth son ambos int -- sin él, Health/MaxHealth sería división
    // entera (truncada a 0 salvo que Health == MaxHealth).
    public float HealthRatio => (float)Health / MaxHealth;
    
    private float _kAmbient   = 0.30f;   
    private float _kDiffuse   = 0.75f;   
    private float _kSpecular  = 0.25f;   
    private float _shininess  = 28.0f; 
    
    // Seteado una sola vez al construirse, igual que IsSolid/IsStatic en Entity
    // -- así ninguna subclase puede "olvidarse" de declarar su equipo.
    public Team Team { get; }

    public Tank(Vector3 position,Vector3 scale, Vector3 rotation, Battlefield battlefield, Team team) : base(position, scale, rotation)
    {
        IsSolid = true;
        IsStatic = false;
        Team = team;

        var (vertices, faces) = BoxColliderFactory.CreateBox(halfWidth: 2f, height: 3f);

        _yaw = 0.0f;
        
        Collider = new ConvexCollider(this, vertices, faces);
        _battlefield = battlefield;
    }

    // Impacto directo de un proyectil -- siempre aplica, no tiene cooldown (un
    // disparo ya está limitado por el cooldown de disparo del que lo tiró).
    // worldHitPoint: punto real de impacto (Projectile.TryHit), en espacio
    // MUNDO. Se convierte a espacio LOCAL del Hull de ESTA instancia (no del
    // Model compartido) invirtiendo su transform actual -- guardado así, el
    // punto queda "pegado" a la malla y sigue al tanque adonde se mueva o
    // gire, en vez de quedar clavado en la posición del mapa donde pegó el
    // disparo (ver memoria del proyecto para la explicación completa).
    // Buffer circular: el impacto más nuevo pisa al más viejo al llenarse.
    public void TakeProjectileDamage(Vector3 worldHitPoint)
    {
        if (IsInvincible) return;
        Health = Math.Max(0, Health - ProjectileDamage);

        if (_hullMesh == null) return;

        var localHitPoint = Vector3.Transform(worldHitPoint, Matrix.Invert(GetHullWorldTransform()));
        _impactPoints[_nextImpactSlot] = localHitPoint;
        _nextImpactSlot = (_nextImpactSlot + 1) % MaxImpactPoints;
        _impactCount = Math.Min(_impactCount + 1, MaxImpactPoints);
    }

    // Choque físico contra otro tanque/prop -- gateado por cooldown (ver comentario
    // de CollisionDamageCooldownSeconds). La llama PhysicsSystem.CheckPair, que
    // decide CUÁNTO (DefaultCollisionDamage contra algo sólido, o lo que pise
    // Prop.TankCollisionDamage contra un prop destructible como Tree).
    public void TakeCollisionDamage(int amount)
    {
        if (IsInvincible) return;
        if (_collisionDamageCooldownTimer > 0f) return;

        Health = Math.Max(0, Health - amount);
        _collisionDamageCooldownTimer = CollisionDamageCooldownSeconds;
    }

    // Espacio de mundo -- la usa CollisionResolver para el clipping de
    // velocidad (cancelar, al chocar, solo la componente que entra al otro
    // objeto). _tankSpeed es escalar (a lo largo de _world.Forward); ver su
    // comentario de declaración.
    public override Vector3 GetVelocity() => _world.Forward * _tankSpeed;

    // La llama CollisionResolver/PhysicsSystem tras resolver un choque, para
    // dejar las dos orugas acordes a la velocidad ya corregida (clip contra
    // sólidos, o frenado parcial contra un prop destructible) -- sin esto,
    // _leftCaterpillarSpeed/_rightCaterpillarSpeed seguirían con el valor
    // acumulado de antes del choque y el tanque volvería a empujar contra el
    // obstáculo en el frame siguiente (issue #11: por eso quedaba trabado
    // perdiendo vida para siempre -- CollisionResolver corregía _position
    // pero nunca esto). scale=1 no cambia nada; scale=0 frena en seco.
    //
    // Frenar la velocidad por sí solo no alcanzaba (confirmado con otra
    // simulación aparte): si algo sigue pidiendo "avanzar" en el mismo
    // sentido frame tras frame (la IA en Ruta hacia un waypoint bloqueado),
    // la velocidad se vuelve a reconstruir de a poquito y el tanque queda
    // tocando el obstáculo sin avanzar de verdad -- igual que antes, solo que
    // más despacio. BlockedSpeedScaleThreshold marca cuándo ese frenado fue
    // lo bastante fuerte como para considerar que el tanque está realmente
    // bloqueado (no un rocecito de costado, que casi no frena) -- IsBlocked
    // es lo que AI.Decide usa para dejar de insistir con el mismo rumbo. El
    // multiplicador fijo de Tree (0.85, ver Tree.cs) queda siempre por
    // encima del umbral a propósito: pasar por el bosque no cuenta como
    // "bloqueado".
    private const float BlockedSpeedScaleThreshold = 0.5f;
    public bool IsBlocked { get; private set; }

    public void ApplyCollisionSpeedScale(float scale)
    {
        _leftCaterpillarSpeed *= scale;
        _rightCaterpillarSpeed *= scale;

        if (scale < BlockedSpeedScaleThreshold)
        {
            IsBlocked = true;
        }
    }

    // correctTurretCannonRoll180/visualGroundOffset: corrigen diferencias de
    // convención entre modelos (hoy hace falta para el T90, no para el
    // Panzer -- ver comentario en GameScene.LoadContent). No tocan _position
    // ni el collider -- son puramente visuales, así que no afectan física,
    // colisiones ni la IA (que siguen viendo al tanque exactamente donde
    // siempre, solo cambia dónde se DIBUJA).
    //
    // correctTurretCannonRoll180, cómo se corrige (ver _t90RigCorrectionInverse
    // arriba): NO se reemplaza el reposo del bone por uno limpio -- eso se
    // probó (StripRotation) y rompía la apariencia real del modelo (la
    // torreta quedaba "parada" en vez de apoyada, confirmado con renders
    // aislados fuera del juego). Se deja el reposo ORIGINAL intacto y se
    // premultiplica por E (capturado antes de nada más) -- eso pasa a ser lo
    // que RotationalObject usa como su propio "reposo". El resultado que
    // RotationalObject calcula cada frame queda, entonces, UN PASO corrido
    // respecto de lo que hace falta mostrar/disparar -- se corrige
    // multiplicando por E⁻¹ cada vez que se LEE el transform (ver
    // GetCannonAbsolute y el dibujo de Turret en DrawMeshes), nunca writeando
    // sobre el bone de nuevo.
    public void LoadContent(Model model, Texture tankTexture, Texture caterpillarTexture,  Effect tankEffect,  string turretBoneName, string canonBoneName, string leftCaterpillarName, string rightCaterpillarName, List<string> leftWheelsNames, List<string> rightWheelNames, string leftDriveSprocket, string rightDriveSprocket, bool correctTurretCannonRoll180 = false, float visualGroundOffset = 0f, string hullBoneName = "Hull")
    {
        _model = model;
        _effect = tankEffect;
        _tankTexture = tankTexture;
        _caterpillarTexture = caterpillarTexture;
        _visualGroundOffset = visualGroundOffset;

        _absoluteWorldsMatrix = new Matrix[model.Bones.Count];

        var turretOriginalLocal = Matrix.Identity;

        foreach (var bone in _model.Bones)
        {
            if (bone.Name == turretBoneName)
            {
                if (correctTurretCannonRoll180)
                {
                    turretOriginalLocal = bone.Transform;
                    bone.Transform = ApplyT90RigCorrection(bone.Transform, out _t90RigCorrectionInverse);
                }
                _turret = new Turret(bone);
            }
            else if (bone.Name == canonBoneName)
            {
                if (correctTurretCannonRoll180)
                {
                    _cannonOriginalLocal = bone.Transform;
                    // Mismo E que Turret (comparten la misma rotación de reposo,
                    // confirmado inspeccionando los dos bones) -- no hace falta
                    // guardar este E⁻¹ aparte, ya quedó en _t90RigCorrectionInverse.
                    bone.Transform = ApplyT90RigCorrection(bone.Transform, out _);
                }
                _canon = new Canon(bone);
            }
            else if (leftWheelsNames.Contains(bone.Name))
            {
                _leftWheelBones.Add(bone);
            }
            else if (rightWheelNames.Contains(bone.Name))
            {
                _rightWheelBones.Add(bone);
            }
        }

        _generalMeshes = _model.Meshes.Where(m => m.Name != leftCaterpillarName && m.Name != rightCaterpillarName).ToList();

        foreach (var generalMesh in _generalMeshes)
        {
            foreach (var meshPart in generalMesh.MeshParts)
            {
                meshPart.Effect = tankEffect;
            }
        }

        // Sin condicionar a correctTurretCannonRoll180 -- GetCanonFiringDirection
        // lo necesita para CUALQUIER modelo (Panzer incluido), no solo T90.
        _cannonMesh = _model.Meshes.FirstOrDefault(m => m.Name == canonBoneName);

        // Confirmado inspeccionando el .fbx crudo de los dos modelos: el
        // nodo se llama "Hull" en Panzer y en T90 por igual (default del
        // parámetro) -- a diferencia de Turret/Cannon no hace falta que
        // GameScene lo pase explícito, pero queda como parámetro por las
        // dudas de que algún modelo futuro lo llame distinto.
        _hullMesh = _model.Meshes.FirstOrDefault(m => m.Name == hullBoneName);

        // T90: Cannon NO es hijo de Turret en la jerarquía real del modelo
        // (confirmado inspeccionando los bones -- en el Panzer sí lo es), así
        // que al yawear la torreta el caño no la sigue. No se puede reparentar
        // un ModelBone en tiempo de ejecución, así que se compensa a mano acá:
        // ver _compensateCannonYaw / GetCannonCompensatedAbsolute.
        if (correctTurretCannonRoll180)
        {
            _compensateCannonYaw = true;
            _cannonRelativeToTurret = _cannonOriginalLocal * Matrix.Invert(turretOriginalLocal);
            _turretMesh = _model.Meshes.FirstOrDefault(m => m.Name == turretBoneName);
        }
        
        _leftDriveSprocket = _model.Bones.FirstOrDefault(m => m.Name == leftDriveSprocket);
        _rightDriveSprocket = _model.Bones.FirstOrDefault(m => m.Name == rightDriveSprocket);
        
        _leftCaterpillarMesh = _model.Meshes.FirstOrDefault(m => m.Name == leftCaterpillarName);
    
        foreach (var meshPart in _leftCaterpillarMesh.MeshParts)
        {
            meshPart.Effect = _effect; 
        }
        
        _rightCaterpillarMesh = _model.Meshes.FirstOrDefault(m => m.Name == rightCaterpillarName);

        foreach (var meshPart in _rightCaterpillarMesh.MeshParts)
        {
            meshPart.Effect = _effect;
        }

        // Al final de todo -- Turret/Canon recién construidos, ángulo
        // dinámico en 0 (todavía no corrió ningún Update real), así que esto
        // es el reposo real del modelo. Ver CalibrateFiringDirection.
        CalibrateFiringDirection();

        if (_hullMesh != null)
        {
            // length() de la primera fila: para una escala uniforme (que es
            // lo que tiene el Hull en los dos modelos -- confirmado, M11=
            // M22=M33) esto da exactamente ese factor. Se calcula una sola
            // vez acá (no en cada impacto ni en el shader) -- ver el
            // comentario de _hullLocalScale más arriba.
            var hullWorld = GetHullWorldTransform();
            var row0 = new Vector3(hullWorld.M11, hullWorld.M12, hullWorld.M13);
            _hullLocalScale = MathF.Max(row0.Length(), 0.0001f);
        }
    }

    public Turret GetTurret()
    {
        return _turret;
    }

    public Canon GetCanon()
    {
        return _canon;
    }

    /// <summary>
    ///     Yaw del casco en espacio de mundo. La torreta gira en relación a este valor.
    /// </summary>
    public float GetHullYaw()
    {
        return _yaw;
    }
    

    public virtual void Update(float elapsedTime)
    {
        // Se resetea ACÁ (antes de que la física de este frame pueda volver a
        // marcarlo) y no en PhysicsSystem -- así, si este frame no hay ningún
        // choque fuerte, el próximo AI.Decide (que corre ANTES que este
        // Update, ver AITank.Update) ya lo lee en false. Si sí lo hay,
        // ApplyCollisionSpeedScale lo vuelve a poner en true más tarde en
        // este mismo frame, a tiempo para el AI.Decide del frame siguiente.
        IsBlocked = false;

        _collisionDamageCooldownTimer -= elapsedTime;

        _leftCaterpillarSpeed += _speedingDirection * _acceleration * elapsedTime;
        _rightCaterpillarSpeed += _speedingDirection * _acceleration * elapsedTime;
        
        _leftCaterpillarSpeed -= _rotationDirection * _deceleration * elapsedTime;
        _rightCaterpillarSpeed += _rotationDirection * _deceleration * elapsedTime;
        
        // Está fórmula de amortiguación es equivalente a  Vf = Vi / (1 + C * dT) 
        // Es muy útil debido a que permite acotar la velocidad de movimiento del objeto (Impone una Velocidad terminal)
        _leftCaterpillarSpeed /= (1.0f + _dumping * elapsedTime);  
        _rightCaterpillarSpeed /= (1.0f + _dumping * elapsedTime);
        
        float angularSpeed = (_leftCaterpillarSpeed -  _rightCaterpillarSpeed) / _caterpillarDistance ;
        _tankSpeed = (_leftCaterpillarSpeed + _rightCaterpillarSpeed) / 2;

        float difAngle = angularSpeed * elapsedTime;
        _yaw += difAngle;

        _position += _world.Forward * _tankSpeed * elapsedTime;

        _leftCaterpillarPosition += _leftCaterpillarSpeed * _caterpillarTextureScale * elapsedTime;
        _rightCaterpillarPosition += _rightCaterpillarSpeed * _caterpillarTextureScale * elapsedTime;
        
        const float floorDistance = 0.1f; 
        _position.Y = _battlefield.GetHeightAt(_position.X, _position.Z) + floorDistance ;
        
        Quaternion tankRotation = Quaternion.CreateFromYawPitchRoll(difAngle, 0,0 );
        Quaternion terrainRotation = _getTerrainRotationQuaternion(); 
        
        _rotation = tankRotation * terrainRotation;
        
        _setWheelsRotation(elapsedTime);
        
        SetWorldMatrix();

        
        _turret.Update(elapsedTime);
        _canon.Update(elapsedTime);
    }

    // Transform absoluto del bone del cañón, en espacio de mundo -- de ahí sale
    // la posición y dirección de spawn de un proyectil. Se recalcula fresco acá
    // (no se reusa _absoluteWorldsMatrix de DrawMeshes) porque Update corre antes
    // que Draw en el frame: si WantsToFire se decide en este Update, el último
    // valor dibujado todavía sería el del frame anterior.
    public Matrix GetCanonWorldTransform()
    {
        _model.CopyAbsoluteBoneTransformsTo(_absoluteWorldsMatrix);
        return GetCannonAbsolute() * GetVisualWorld();
    }

    // Mismo motivo que GetCanonWorldTransform de arriba (recalcular fresco,
    // no reusar lo que dejó el último Draw) -- la usa TakeProjectileDamage,
    // que corre en Update, para convertir el punto de impacto (mundo) a
    // espacio local del Hull. El Hull no tiene corrección de rig como
    // Turret/Cannon -- es un bone normal, incluso en el T90.
    private Matrix GetHullWorldTransform()
    {
        _model.CopyAbsoluteBoneTransformsTo(_absoluteWorldsMatrix);
        return _absoluteWorldsMatrix[_hullMesh.ParentBone.Index] * GetVisualWorld();
    }

    // Dirección real de disparo -- NO se calcula leyendo .Forward del
    // transform de arriba. Para el T90, esa matriz combinada (yaw+pitch,
    // ver GetCannonAbsolute) termina con escala NO uniforme (confirmado:
    // .Forward sale con longitud ~100 en vez de 1) -- la malla se sigue
    // dibujando bien porque DrawMeshes transforma cada VÉRTICE real con esa
    // matriz (siempre da la posición correcta, tenga o no escala rara), pero
    // ".Forward" es un atajo que lee una sola fila de la matriz asumiendo
    // que es una rotación limpia -- con escala no uniforme esa lectura deja
    // de representar una dirección real, aunque la malla se vea perfecta.
    //
    // Por eso acá se transforman DOS PUNTOS reales de la propia malla del
    // caño (su origen y el borde de su propia BoundingSphere, calculada por
    // el pipeline de contenido a partir de los vértices reales) con la
    // MISMA matriz que ya usa el dibujo -- transformar puntos siempre da la
    // posición correcta sin importar la escala de la matriz, a diferencia
    // de leer una fila suelta. Funciona igual para Panzer (sin este
    // problema) que para T90.
    public Vector3 GetCanonFiringDirection()
    {
        _model.CopyAbsoluteBoneTransformsTo(_absoluteWorldsMatrix);
        var combined = GetCannonAbsolute() * GetVisualWorld();

        var breech = Vector3.Transform(Vector3.Zero, combined);
        var muzzle = Vector3.Transform(_calibratedLocalMuzzle, combined);

        var direction = muzzle - breech;
        direction.Normalize();
        return direction;
    }

    // Punto "muzzle" en espacio LOCAL del bone del cañón -- calibrado una
    // sola vez (ver CalibrateFiringDirection, llamado al final de
    // LoadContent) en vez de adivinado con la BoundingSphere de la malla.
    // Una primera versión usaba directamente el centro de la BoundingSphere
    // como punto de referencia -- medido contra el Panzer (el único modelo
    // donde hay una referencia 100% confiable para comparar, -Forward, que
    // nunca tuvo el problema de escala del T90), daba un error FIJO de
    // 8.28° en los 20 ángulos de yaw/pitch probados -- ni uno más ni uno
    // menos, siempre igual, lo que confirma que no es ruido: la malla del
    // caño no es simétrica alrededor de su propio eje de disparo (la culata
    // pesa más que el tubo), así que el centro de su esfera envolvente
    // queda corrido del eje real.
    private Vector3 _calibratedLocalMuzzle;

    // Encuentra, en espacio LOCAL del cañón, el punto que -- al pasar por
    // la transformación completa de HOY (GetCannonAbsolute * GetVisualWorld)
    // -- da la dirección de referencia correcta. Se llama en reposo real
    // (ángulo dinámico de Turret/Canon en 0, antes de la primera llamada a
    // su Update) para que la referencia sea inequívoca: en reposo, el caño
    // tiene que apuntar hacia donde mira el CASCO -- eso vale para
    // CUALQUIER modelo (no hace falta un -Forward confiable, que es
    // justamente lo que falla en el T90), así que la misma calibración
    // sirve para los dos sin casos especiales por modelo.
    private void CalibrateFiringDirection()
    {
        _model.CopyAbsoluteBoneTransformsTo(_absoluteWorldsMatrix);
        var combinedAtRest = GetCannonAbsolute() * GetVisualWorld();

        var breech = Vector3.Transform(Vector3.Zero, combinedAtRest);

        // Misma convención invertida que ya usa el resto del proyecto
        // (Move(-1) es "avanzar" -- el modelo queda mirando al revés de
        // _world.Forward, ver comentario histórico de TrySpawnProjectile).
        var reference = -_world.Forward;
        reference.Normalize();

        var distance = MathF.Max(_cannonMesh.BoundingSphere.Radius, 0.1f);
        var referenceWorldPoint = breech + reference * distance;

        _calibratedLocalMuzzle = Vector3.Transform(referenceWorldPoint, Matrix.Invert(combinedAtRest));
    }

    // _world corregido con el offset vertical puramente visual (ver
    // LoadContent) -- se usa para TODO lo que se dibuja o se calcula a
    // partir de dónde se ve la malla (incluida la posición de disparo del
    // cañón), nunca para _position/colisiones/IA, que siguen usando _world
    // sin corregir.
    private Matrix GetVisualWorld()
    {
        return Matrix.CreateTranslation(0, _visualGroundOffset, 0) * _world;
    }

    // Transform absoluto del Turret, con la corrección real del T90 aplicada
    // (ver _t90RigCorrectionInverse) -- usado tanto para dibujar la torreta
    // como para calcular dónde queda el Cannon (GetCannonAbsolute). Sin
    // _compensateCannonYaw (Panzer/aliados), es simplemente el absoluto de
    // siempre, sin tocar.
    private Matrix GetTurretAbsolute()
    {
        var raw = _absoluteWorldsMatrix[_turret.BoneIndex];
        return _compensateCannonYaw ? _t90RigCorrectionInverse * raw : raw;
    }

    // Transform absoluto del Cannon. Cannon no está anidado bajo Turret en la
    // jerarquía real del T90 (son hermanos, ver LoadContent), así que no
    // alcanza con _absoluteWorldsMatrix[_canon.BoneIndex] -- se reconstruye a
    // mano, pegado a la posición ya corregida de Turret (_cannonRelativeToTurret),
    // con el pitch dinámico aplicado como una rotación pura que pivotea en el
    // origen local del propio bone del caño (no en el de la torreta): se
    // extrae comparando el bone de Cannon YA actualizado por Canon.Update()
    // (que sigue rotando normalmente, en su propio eje remapeado por E, ver
    // ApplyT90RigCorrection) contra su reposo crudo. Validado con renders
    // aislados (yaw+pitch combinados) antes de portar acá -- ver memoria del
    // proyecto.
    private Matrix GetCannonAbsolute()
    {
        if (!_compensateCannonYaw)
        {
            return _absoluteWorldsMatrix[_canon.BoneIndex];
        }

        var cannonLocalCorrected = _model.Bones[_canon.BoneIndex].Transform;
        var pitchCorrectionLocal = _t90RigCorrectionInverse * cannonLocalCorrected * Matrix.Invert(_cannonOriginalLocal);

        return pitchCorrectionLocal * _cannonRelativeToTurret * GetTurretAbsolute();
    }


    // El padre REAL de Turret/Cannon en el T90 no es Hull/RootNode sino un
    // nodo "Empty" con su propia rotación D (confirmado inspeccionando ese
    // bone directamente). El giro dinámico de yaw/pitch (Turret/Canon.
    // GetRotation, código de los compañeros, no tocable) siempre rota
    // alrededor de los ejes X/Y estándar sin saber nada de Empty -- un yaw
    // (eje Y) termina, al pasar por la rotación de Empty, actuando sobre un
    // eje horizontal en vez de vertical (se veía como si inclinara en vez de
    // girar). Reemplazar el reposo por uno "limpio" (intentado antes,
    // StripRotation) arregla el EJE pero rompe la apariencia real del
    // modelo, porque el reposo real NO es limpio -- tiene la rotación D de
    // taller.
    //
    // La corrección que sí funciona (validada con renders aislados, en
    // reposo y en movimiento, comparando contra el Panzer y contra el
    // modelo crudo antes de tocar nada): dejar el reposo ORIGINAL intacto y
    // premultiplicarlo por E = D * RotX(90°) ANTES de dárselo a
    // RotationalObject como su "_originalWorldTransform". Por la identidad
    // de conjugación de rotaciones (RotY(yaw) alrededor del eje Y mapeado
    // por E es lo mismo que E⁻¹ * RotY(yaw) * E alrededor del eje Y
    // estándar), lo que RotationalObject termina calculando cada frame
    // queda "E de más" -- se cancela multiplicando por E⁻¹ cada vez que se
    // LEE el resultado (GetTurretAbsolute/GetCannonAbsolute), nunca
    // escribiendo sobre el bone de nuevo.
    private static Matrix ApplyT90RigCorrection(Matrix originalTransform, out Matrix correctionInverse)
    {
        originalTransform.Decompose(out _, out var rotation, out _);
        var e = Matrix.CreateFromQuaternion(rotation) * Matrix.CreateRotationX(MathHelper.PiOver2);
        correctionInverse = Matrix.Invert(e);
        return e * originalTransform;
    }

    private void _setWheelsRotation(float elapsedTime)
    {
        float difAngleRightWheels = (_leftCaterpillarSpeed / _wheelRadius) * elapsedTime;
        float difAngleLeftWheels = (_rightCaterpillarSpeed / _wheelRadius) * elapsedTime;
        
        Matrix rotationRightWheels = Matrix.CreateFromYawPitchRoll(0, difAngleRightWheels, 0);
        Matrix rotationLeftWheels = Matrix.CreateFromYawPitchRoll(0, difAngleLeftWheels, 0);
        
        _rightWheelBones.ForEach(w => w.Transform = Matrix.Transpose(rotationRightWheels) * w.Transform);
        _leftWheelBones.ForEach(w => w.Transform = Matrix.Transpose(rotationLeftWheels) * w.Transform);
        
        _rightDriveSprocket.Transform = rotationRightWheels * _rightDriveSprocket.Transform;
        _leftDriveSprocket.Transform = rotationLeftWheels * _leftDriveSprocket.Transform;
    }
    
    private Quaternion _getTerrainRotationQuaternion()
    {
        Vector3 normalTerreno = _battlefield.GetNormalAt(_position.X, _position.Z);
        
        Vector3 right = Vector3.Cross(_world.Forward, normalTerreno);
        right.Normalize();
        
        Vector3 foward = Vector3.Cross(normalTerreno, right);
        foward.Normalize();
        
        Matrix rotacionTerreno = Matrix.Identity;
        rotacionTerreno.Forward = foward;
        rotacionTerreno.Right = right;
        rotacionTerreno.Up = normalTerreno;
        
        return Quaternion.CreateFromRotationMatrix(rotacionTerreno);
    }
    
    protected void Move(int direction)
    {
        _speedingDirection = direction;
    }
    
    protected void Rotate(int direction)
    {
        _rotationDirection = direction;
    }

    public void Draw(GraphicsDevice graphicsDevice, BaseCamera camera, Matrix projection, Light light)
    {
        _effect.Parameters["Projection"].SetValue(projection);
        _effect.Parameters["View"].SetValue(camera.GetView());
        
        _effect.Parameters["LightPosition"].SetValue(light.Position);
        _effect.Parameters["AmbientColor"].SetValue(light.AmbientColor);
        _effect.Parameters["DiffuseColor"].SetValue(light.DiffuseColor);
        _effect.Parameters["SpecularColor"].SetValue(light.SpecularColor);
        
        _effect.Parameters["KAmbient"].SetValue(_kAmbient);
        _effect.Parameters["KDiffuse"].SetValue(_kDiffuse);
        _effect.Parameters["KSpecular"].SetValue(_kSpecular);
        _effect.Parameters["Shininess"].SetValue(_shininess);
        
        _effect.Parameters["CameraPosition"].SetValue(camera.Position);
        
        
        DrawMeshes(graphicsDevice);
    }
    
    protected override void DrawMeshes(GraphicsDevice graphicsDevice)
    {
        _effect.Parameters["ModelTexture"].SetValue(_tankTexture);
        _effect.Parameters["TexturePositions"].SetValue(new Vector2(0,0));

        // Por defecto, la técnica SIN deformación -- la misma que corría
        // antes de esta feature para TODO. Solo el Hull (y solo si tiene
        // impactos) la cambia más abajo, puntualmente para su propio Draw.
        _effect.CurrentTechnique = _effect.Techniques["BasicColorDrawing"];

        _model.CopyAbsoluteBoneTransformsTo(_absoluteWorldsMatrix);
        var visualWorld = GetVisualWorld();
        foreach (var mesh in _generalMeshes)
        {
            Matrix meshAbsolute;
            if (mesh == _cannonMesh)
            {
                meshAbsolute = GetCannonAbsolute();
            }
            else if (mesh == _turretMesh)
            {
                meshAbsolute = GetTurretAbsolute();
            }
            else
            {
                meshAbsolute = _absoluteWorldsMatrix[mesh.ParentBone.Index];
            }
            Matrix meshWorld = meshAbsolute * visualWorld;

            _effect.Parameters["World"].SetValue(meshWorld);
            _effect.Parameters["InverseTransposeWorld"].SetValue(Matrix.Transpose(Matrix.Invert(meshWorld)));

            // Solo el Hull, y solo si ya recibió algún impacto, pasa a la
            // técnica DeformedDrawing (ver su comentario en ModelShader.fx
            // para la saga completa de por qué es una técnica aparte y no
            // un uniform más en la de siempre). Para cualquier otra malla
            // -- Turret/Cannon/ruedas -- ni se toca _effect.CurrentTechnique,
            // sigue en BasicColorDrawing, que ni sabe que esto existe.
            if (mesh == _hullMesh && _impactCount > 0)
            {
                _effect.CurrentTechnique = _effect.Techniques["DeformedDrawing"];
                _effect.Parameters["ImpactLocalScale"].SetValue(_hullLocalScale);
                _effect.Parameters["ImpactCount"].SetValue((float) _impactCount);
                for (var i = 0; i < MaxImpactPoints; i++)
                {
                    _impactPointsUpload[i] = i < _impactCount ? new Vector4(_impactPoints[i], 0f) : Vector4.Zero;
                }
                _effect.Parameters["ImpactPoints"].SetValue(_impactPointsUpload);
            }

            mesh.Draw();

            // Vuelve a la técnica de siempre apenas termina de dibujar el
            // Hull -- nada más del tanque (ruedas, orugas) debe heredarla.
            if (mesh == _hullMesh)
            {
                _effect.CurrentTechnique = _effect.Techniques["BasicColorDrawing"];
            }
        }

        _drawCatterpillars(graphicsDevice);
    }

    private void _drawCatterpillars(GraphicsDevice graphicsDevice)
    {
        var visualWorld = GetVisualWorld();

        // CurrentTechnique ya está en BasicColorDrawing (DrawMeshes la
        // vuelve a dejar así apenas termina de dibujar el Hull) -- las
        // orugas ni se enteran de que la deformación existe.
        _effect.Parameters["ModelTexture"].SetValue(_caterpillarTexture);

        _effect.Parameters["TexturePositions"].SetValue(new Vector2(0, _leftCaterpillarPosition));

        Matrix leftCaterpillarWorld = _absoluteWorldsMatrix[_leftCaterpillarMesh.ParentBone.Index] * visualWorld;
        _effect.Parameters["World"].SetValue(leftCaterpillarWorld);
        _effect.Parameters["InverseTransposeWorld"].SetValue(Matrix.Transpose(Matrix.Invert(leftCaterpillarWorld)));

        _leftCaterpillarMesh.Draw();


        _effect.Parameters["TexturePositions"].SetValue(new Vector2(0, _rightCaterpillarPosition));

        Matrix rightCaterpillarWorld = _absoluteWorldsMatrix[_rightCaterpillarMesh.ParentBone.Index] * visualWorld;
        _effect.Parameters["World"].SetValue(rightCaterpillarWorld);
        _effect.Parameters["InverseTransposeWorld"].SetValue(Matrix.Transpose(Matrix.Invert(rightCaterpillarWorld)));

        _rightCaterpillarMesh.Draw();
    }
}