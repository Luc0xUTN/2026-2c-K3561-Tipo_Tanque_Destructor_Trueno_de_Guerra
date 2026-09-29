using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP.Tanks;

// Responsabilidad: Encargarse de la dinámica de la rotación de un objeto
public abstract class RotationalObject
{
    private float _targetAngle = 0;

    public float TargetAngle
    {
        get => _targetAngle;
        set => _targetAngle = MathHelper.WrapAngle(value);
    }
    
    private float _angularSpeed;
    protected float _angularAceleration;
    protected float _angle = 0f;
    protected float _angularDamping; 
    

    private int _direction = 0; 
    
    private ModelBone _bone;
    private readonly Matrix _originalWorldTransform;
    
    protected RotationalObject(ModelBone bone)
    {
        _bone = bone;
        _originalWorldTransform = bone.Transform;
    }

    public void Update(float elapsedTime)
    {
        float deltaAngle = MathHelper.WrapAngle(_targetAngle - _angle);
        
        if (Math.Abs(deltaAngle) < 0.005f)
        {
            _angle = _targetAngle;
            _angularSpeed = 0;
        }
        else
        {
            _angularSpeed = _angularSpeed + deltaAngle * _angularAceleration * elapsedTime;
            _angularSpeed /= (1.0f + _angularDamping * elapsedTime);
            _angle = _angle + _angularSpeed * elapsedTime;
        }

        _angle = MathHelper.WrapAngle(_angle);
        
        Quaternion rotation = GetRotation();
        _bone.Transform = Matrix.CreateFromQuaternion(rotation) * _originalWorldTransform;
    }

    protected abstract Quaternion GetRotation();
    
    
    
}