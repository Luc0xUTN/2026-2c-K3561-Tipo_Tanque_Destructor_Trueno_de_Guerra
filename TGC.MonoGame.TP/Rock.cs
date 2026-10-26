using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.Collisions;

namespace TGC.MonoGame.TP;

// Antes las rocas eran un Prop sin collider (no colisionaban con nada) --
// mismo patrón que Tree, caja más baja/achatada en vez de alta y angosta.
public class Rock : Prop
{

    private Color _color;
    
    public Rock(Vector3 position, Vector3 scale, Vector3 rotation, Color color) : base(position, scale, rotation)
    {
        IsSolid = true;
        var (vertices, faces) = BoxColliderFactory.CreateBox(halfWidth: 0.5f, height: 1f);
        Collider = new ConvexCollider(this, vertices, faces);
        _color = color; 
    }
    

    protected override void DrawMeshes(GraphicsDevice graphicsDevice)
    {
        _effect.Parameters["DiffuseColor"].SetValue(_color.ToVector3());
        base.DrawMeshes(graphicsDevice);
    }
}
