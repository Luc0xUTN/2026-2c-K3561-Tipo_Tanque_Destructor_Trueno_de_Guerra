using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP;

// Responsabilidad: Representar a un tanque en sí 
public class Tank : Entity
{
    private float _acceleration = 200f;
    private float _speed = 0; 
    
    
    public Tank(Vector3 position,Vector3 scale, Vector3 rotation, Color color) : base(position, scale, rotation, color )
    {
        
    }
    
    public void Update(float elapsedTime)
    {
        _speed += elapsedTime * _acceleration;
        _position += elapsedTime * _world.Forward * _speed;
        
        SetWorldMatrix();
    }
    
    
    protected override void DrawMeshes()
    {
        foreach (var mesh in _model.Meshes)
        {
            _effect.Parameters["World"].SetValue(mesh.ParentBone.Transform * _world);
            mesh.Draw();
        }
    }
}