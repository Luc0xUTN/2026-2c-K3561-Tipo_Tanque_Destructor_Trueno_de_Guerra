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
    private const int CollisionDamage = 2;
    // Sin este cooldown, un tanque trabado contra un obstáculo (issue #11,
    // todavía sin resolver) perdería 2 de vida CADA FRAME mientras siga
    // superpuesto -- se moriría en menos de un segundo. Esto no limita el daño
    // de un impacto de proyectil (eso siempre aplica, ver TakeProjectileDamage).
    private const float CollisionDamageCooldownSeconds = 0.5f;
    private float _collisionDamageCooldownTimer;

    public int Health { get; private set; } = MaxHealth;
    public bool IsDead => Health <= 0;

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
    public void TakeProjectileDamage()
    {
        Health = Math.Max(0, Health - ProjectileDamage);
    }

    // Choque físico contra otro tanque/prop -- gateado por cooldown (ver comentario
    // de CollisionDamageCooldownSeconds). La llama PhysicsSystem.CheckPair.
    public void TakeCollisionDamage()
    {
        if (_collisionDamageCooldownTimer > 0f) return;

        Health = Math.Max(0, Health - CollisionDamage);
        _collisionDamageCooldownTimer = CollisionDamageCooldownSeconds;
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
    public void LoadContent(Model model, Texture tankTexture, Texture caterpillarTexture,  Effect tankEffect,  string turretBoneName, string canonBoneName, string leftCaterpillarName, string rightCaterpillarName, List<string> leftWheelsNames, List<string> rightWheelNames, string leftDriveSprocket, string rightDriveSprocket, bool correctTurretCannonRoll180 = false, float visualGroundOffset = 0f)
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

        // T90: Cannon NO es hijo de Turret en la jerarquía real del modelo
        // (confirmado inspeccionando los bones -- en el Panzer sí lo es), así
        // que al yawear la torreta el caño no la sigue. No se puede reparentar
        // un ModelBone en tiempo de ejecución, así que se compensa a mano acá:
        // ver _compensateCannonYaw / GetCannonCompensatedAbsolute.
        if (correctTurretCannonRoll180)
        {
            _compensateCannonYaw = true;
            _cannonMesh = _model.Meshes.FirstOrDefault(m => m.Name == canonBoneName);
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
        float tankSpeed = (_leftCaterpillarSpeed + _rightCaterpillarSpeed) / 2;
        
        float difAngle = angularSpeed * elapsedTime;
        _yaw += difAngle; 
        
        _position += _world.Forward * tankSpeed * elapsedTime;

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

            mesh.Draw();
        }
        
        _drawCatterpillars(graphicsDevice);
    }

    private void _drawCatterpillars(GraphicsDevice graphicsDevice)
    {
        var visualWorld = GetVisualWorld();

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