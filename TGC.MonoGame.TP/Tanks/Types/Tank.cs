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
    private float _acceleration = 50f;
    private float _deceleration = 80f;
    private float _maxSpeed = 50f;

    private float _rotationSpeed = MathF.PI * 0.25f;

    private int _speedingDirection = 0;
    private float _currentSpeed = 0;
    
    private int _rotationDirection = 0;
    
    private float _yaw; 
    
    private Matrix[] _absoluteWorldsMatrix;
    
    private Battlefield _battlefield;
    
    protected Turret _turret;
    protected Canon _canon; 
    
    public Tank(Vector3 position,Vector3 scale, Vector3 rotation, Color color, Battlefield battlefield) : base(position, scale, rotation, color )
    {
        IsSolid = true;
        IsStatic = false;

        var vertices = new List<Vector3>
        {
            new(-2, 0, -2),
            new( 2, 0, -2),
            new( 2, 0,  2),
            new(-2, 0,  2),

            new(-2, 3, -2),
            new( 2, 3, -2),
            new( 2, 3,  2),
            new(-2, 3,  2)
        };

        var faces = new List<Face>
        {
            new(vertices, new List<int> { 0, 1, 2, 3 }),
            new(vertices, new List<int> { 0, 4, 5, 1 }),
            new(vertices, new List<int> { 4, 7, 6, 5 }),
            new(vertices, new List<int> { 1, 5, 6, 2 }),
            new(vertices, new List<int> { 2, 6, 7, 3 }),
            new(vertices, new List<int> { 3, 7, 4, 0 })
        };

        _yaw = 0.0f; 
        
        Collider = new ConvexCollider(this, vertices, faces);
        _battlefield = battlefield;
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
        // Yaw
        var prevYaw = _yaw; 
        _yaw -= _rotationDirection * _rotationSpeed * elapsedTime; // Resta para que el positivo sea hacia las agujas del reloj
        
        var pi2 = MathF.PI * 2;
        
        if (_yaw> pi2)
        {
            _yaw -= pi2;
        }

        Quaternion rotacionTanque = Quaternion.CreateFromYawPitchRoll(_yaw - prevYaw, 0, 0);
        
        if (_speedingDirection != 0) // Está acelerando hacia adelante o atrás
        {
            _currentSpeed = MathF.MinMagnitude(
                _currentSpeed + _acceleration * _speedingDirection * elapsedTime,
                _maxSpeed * _speedingDirection
            );
        }

        int movingDirection = Math.Sign(_currentSpeed);
        float absSpeed = MathF.Abs(_currentSpeed);
        if (_speedingDirection != movingDirection) // Está frenando o está desplazándose pero sin acelerar
        {
            _currentSpeed = MathF.Max(absSpeed - _deceleration * elapsedTime, 0) * movingDirection;
        }
        
        _position += _world.Forward * (_currentSpeed * elapsedTime);
        _position.Y = _battlefield.GetHeightAt(_position.X, _position.Z);

        
        Vector3 normalTerreno = _battlefield.GetNormalAt(_position.X, _position.Z);
        
        Vector3 right = Vector3.Cross(_world.Forward, normalTerreno);
        right.Normalize();
        
        Vector3 foward = Vector3.Cross(normalTerreno, right);
        foward.Normalize();
        
        Matrix rotacionTerreno = Matrix.Identity;
        rotacionTerreno.Forward = foward;
        rotacionTerreno.Right = right;
        rotacionTerreno.Up = normalTerreno;
        
        _rotation = rotacionTanque * Quaternion.CreateFromRotationMatrix(rotacionTerreno);
            
        SetWorldMatrix();

        
        _turret.Update(elapsedTime);
        _canon.Update(elapsedTime);
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