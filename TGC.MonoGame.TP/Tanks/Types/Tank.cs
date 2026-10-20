using System;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.Collisions;
using TGC.MonoGame.TP.Tanks;
using Quaternion = Microsoft.Xna.Framework.Quaternion;
using Vector3 = Microsoft.Xna.Framework.Vector3;

namespace TGC.MonoGame.TP;

// Responsabilidad: Representar a un tanque en sí 
public class Tank : Entity
{
    // La velocidad terminal del modelo de orugas amortiguadas es ~accel/_dumping
    // (ver el comentario de Update más abajo) -- con _dumping=2 en el constructor,
    // 100 da ~50u/s, el doble que antes (era 50f ~= 25u/s). Afecta a todos los
    // tanques por igual (Tank es la base de PlayerTank y EnemyTank).
    private float _acceleration = 100f;
    private float _deceleration = 3f; // no es frenado lineal -- controla qué tan brusco gira (diferencial entre orugas).


    private int _speedingDirection = 0;
    private int _rotationDirection = 0;

    private float _leftCaterpillarSpeed = 0;
    private float _rightCaterpillarSpeed = 0;
    private float _dumping = 1.0f;
    
    private float _caterpillarDistance = 2.0f; 
    
    
    private float _yaw; 
    
    private Matrix[] _absoluteWorldsMatrix;
    
    private Battlefield _battlefield;
    
    protected Turret _turret;
    protected Canon _canon;

    // Seteado por PlayerTank/EnemyTank cada Update -- GameScene lo lee después
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

    public Tank(Vector3 position,Vector3 scale, Vector3 rotation, Color color, Battlefield battlefield) : base(position, scale, rotation, color )
    {
        IsSolid = true;
        IsStatic = false;

        var (vertices, faces) = BoxColliderFactory.CreateBox(halfWidth: 2f, height: 3f);

        _yaw = 0.0f;
        _dumping = 2.0f;

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

    public void LoadContent(Model model,  Effect effect, string turretBoneName, string canonBoneName)
    {
        base.LoadContent(model, effect);

        _absoluteWorldsMatrix = new Matrix[model.Bones.Count]; 
        
        foreach (var bone in _model.Bones)
        {
            if (bone.Name == turretBoneName)
            {
                _turret = new Turret(bone);
            }
            else if (bone.Name == canonBoneName)
            {
                _canon = new Canon(bone);
            }
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
        
        const float floorDistance = 0.1f; 
        _position.Y = _battlefield.GetHeightAt(_position.X, _position.Z) + floorDistance ;
        
        Quaternion tankRotation = Quaternion.CreateFromYawPitchRoll(difAngle, 0,0 );
        Quaternion terrainRotation = _getTerrainRotationQuaternion(); 
        
        _rotation = tankRotation * terrainRotation;
            
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
        return _absoluteWorldsMatrix[_canon.BoneIndex] * _world;
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
    
    protected override void DrawMeshes()
    {
        _model.CopyAbsoluteBoneTransformsTo(_absoluteWorldsMatrix); 
        foreach (var mesh in _model.Meshes)
        {
            _effect.Parameters["World"].SetValue(_absoluteWorldsMatrix[mesh.ParentBone.Index] * _world);
            mesh.Draw();
        }
    }
}