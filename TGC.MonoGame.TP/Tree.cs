
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.Collisions;

namespace TGC.MonoGame.TP;

public class Tree : Prop
{
    public Tree(Vector3 position, Vector3 scale, Vector3 rotation) : base(position, scale, rotation)
    {
        IsSolid = true;

    }

    public void Initialize()
    {
        base.Initialize();
        var (vertices, faces) = BoxColliderFactory.CreateBox(halfWidth: 0.5f, height: 3f);
        Collider =  new ConvexCollider(this, vertices, faces);
        BoundingSphere = new BoundingSphere(_position, Collider.BoundingRadius);
    }
    protected override void DrawMeshes(GraphicsDevice graphicsDevice)
    {
        graphicsDevice.BlendState = BlendState.Opaque;
        graphicsDevice.DepthStencilState = DepthStencilState.Default;
        
        base.DrawMeshes(graphicsDevice);
    }
}
