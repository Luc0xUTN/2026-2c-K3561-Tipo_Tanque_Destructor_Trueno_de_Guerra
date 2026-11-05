using System;
using System.Collections.Generic;
using System.Net.Mime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.Cameras;
using TGC.MonoGame.TP.Collisions;

namespace TGC.MonoGame.TP;

// Responsabilidad: Representa entidades que no se mueven por sus propios medios 
public class Prop : Entity
{
    protected float _kAmbient;
    protected float _kDiffuse;
    protected float _kSpecular;
    protected float _shininess;  
    
    private List<Texture> _textures = new List<Texture>();
    public BoundingSphere BoundingSphere { get; protected set; }
    
    public Prop(Vector3 position, Vector3 scale, Vector3 rotation, float kAmbient, float kDiffuse, float kSpecular, float shininess) : base(position, scale, rotation)
    {
        IsStatic = true;
        _kAmbient = kAmbient;
        _kDiffuse = kDiffuse;
        _kSpecular = kSpecular;
        _shininess = shininess; 
    }

    // Comportamiento al chocar con un tanque -- default = sólido de verdad
    // (como ya se comportaban todos los props, Rock incluida: no se destruye,
    // frena al tanque en seco contra la normal del choque, daño "normal").
    // Tree pisa los tres para comportarse distinto (ver Tree.cs). Vive acá y
    // no como chequeos de tipo en PhysicsSystem/CollisionResolver porque es
    // el prop quien sabe qué le pasa a él mismo al ser chocado -- esos dos
    // solo necesitan preguntar, no necesitan conocer cada tipo concreto.
    public virtual bool DestroyedOnTankCollision => false;

    // Solo tiene efecto cuando DestroyedOnTankCollision es true -- un prop
    // sólido (Rock) se resuelve con el clipping de velocidad normal
    // (CollisionResolver), no con un multiplicador fijo.
    public virtual float TankCollisionSpeedMultiplier => 1f;

    public virtual int TankCollisionDamage => Tank.DefaultCollisionDamage;

    public void LoadContent(Model model, Effect effect, List<Texture> texture)
    {
        _textures = texture;
        
        base.LoadContent(model, effect);
    }

    public virtual void Draw(GraphicsDevice graphicsDevice, BaseCamera camera, Matrix projection, Light light)
    {
        _effect.Parameters["Projection"]?.SetValue(projection);
        _effect.Parameters["View"]?.SetValue(camera.GetView());

        _effect.Parameters["LightPosition"]?.SetValue(light.Position);
        _effect.Parameters["AmbientColor"]?.SetValue(light.AmbientColor);
        _effect.Parameters["DiffuseColor"]?.SetValue(light.DiffuseColor);
        _effect.Parameters["SpecularColor"]?.SetValue(light.SpecularColor);
        
        _effect.Parameters["KAmbient"]?.SetValue(_kAmbient);
        _effect.Parameters["KDiffuse"]?.SetValue(_kDiffuse);
        _effect.Parameters["KSpecular"]?.SetValue(_kSpecular);
        _effect.Parameters["Shininess"]?.SetValue(_shininess);
        
        _effect.Parameters["CameraPosition"]?.SetValue(camera.Position);
        
        this.DrawMeshes(graphicsDevice);
    }    
    
    protected override void DrawMeshes(GraphicsDevice graphicsDevice)
    {
        foreach (var mesh in _model.Meshes)
        {
            Matrix meshWorld = mesh.ParentBone.Transform * _world;
            _effect.Parameters["World"].SetValue(meshWorld);
            _effect.Parameters["InverseTransposeWorld"]?.SetValue(Matrix.Transpose(Matrix.Invert(meshWorld)));
            
            for (int i = 0; i < mesh.MeshParts.Count; i++)
            {
                ModelMeshPart part = mesh.MeshParts[i];        
                
                if (i < _textures.Count)
                {
                    _effect.Parameters["ModelTexture"]?.SetValue(_textures[i]);
                }
                
                _effect.CurrentTechnique.Passes[0].Apply(); 
                graphicsDevice.SetVertexBuffer(part.VertexBuffer);
                graphicsDevice.Indices = part.IndexBuffer;
                
                graphicsDevice.DrawIndexedPrimitives(
                    PrimitiveType.TriangleList,
                    part.VertexOffset,
                    part.StartIndex, 
                    part.PrimitiveCount
                );
            }
        }
    }
    
}
