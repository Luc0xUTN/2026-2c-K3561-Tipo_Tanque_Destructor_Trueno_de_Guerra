using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP;

// Responsabilidad: Representa entidades que no se mueven por sus propios medios 
public class Prop : Entity
{
    public Prop(Vector3 position, Vector3 scale, Vector3 rotation, Color color) : base(position, scale, rotation, color)
    {
        
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
