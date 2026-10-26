
using Microsoft.Xna.Framework;
using TGC.MonoGame.TP.Collisions;

namespace TGC.MonoGame.TP;

public class Tree : Prop
{
    public Tree(Vector3 position, Vector3 scale, Vector3 rotation) : base(position, scale, rotation)
    {
        IsSolid = true;
        var (vertices, faces) = BoxColliderFactory.CreateBox(halfWidth: 0.5f, height: 3f);
        Collider = new ConvexCollider(this, vertices, faces);
    }
}
