using System;
using System.Collections.Generic;
using System.Net.Mime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.Collisions;

namespace TGC.MonoGame.TP;

// Responsabilidad: Representa entidades que no se mueven por sus propios medios 
public class Prop : Entity
{
    private List<Texture> _textures = new List<Texture>();
    public BoundingSphere BoundingSphere { get; protected set; }
    
    public Prop(Vector3 position, Vector3 scale, Vector3 rotation) : base(position, scale, rotation)
    {
        IsStatic = true;
        
    }

    public void LoadContent(Model model, Effect effect, List<Texture> texture)
    {
        _textures = texture;
        
        base.LoadContent(model, effect);
    }

    
    protected override void DrawMeshes(GraphicsDevice graphicsDevice)
    {
        foreach (var mesh in _model.Meshes)
        {
            _effect.Parameters["World"].SetValue(mesh.ParentBone.Transform * _world);
            
            for (int i = 0; i < mesh.MeshParts.Count; i++)
            {
                ModelMeshPart part = mesh.MeshParts[i];        
                
                if (i < _textures.Count)
                {
                    _effect.Parameters["ModelTexture"].SetValue(_textures[i]);
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
