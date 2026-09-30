using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.Collisions;
using TGC.MonoGame.TP.Tanks;
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

    private Matrix[] _absoluteWorldsMatrix;

    protected Turret _turret;
    protected Canon _canon; 
    
    public Tank(Vector3 position,Vector3 scale, Vector3 rotation, Color color) : base(position, scale, rotation, color )
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

        Collider = new ConvexCollider(this, vertices, faces);
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
        return _rotation.X;
    }
    

    public virtual void Update(float elapsedTime)
    {

        // Yaw
        _rotation.X -= _rotationDirection * _rotationSpeed * elapsedTime; // Resta para que el positivo sea hacia las agujas del reloj
        var pi2 = MathF.PI * 2;
        if (_rotation.X > pi2)
        {
            _rotation.X -= pi2;
        }


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