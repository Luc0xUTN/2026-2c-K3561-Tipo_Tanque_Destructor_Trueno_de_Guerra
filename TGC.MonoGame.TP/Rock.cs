using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.Collisions;

namespace TGC.MonoGame.TP;

// Antes las rocas eran un Prop sin collider (no colisionaban con nada) --
// mismo patrón que Tree, caja más baja/achatada en vez de alta y angosta.
public class Rock : Prop
{

    public int RockType { get; }
    private Color _color;

    // Límites exactos medidos para cada uno de los 4 modelos de roca (Rock0 a Rock3)
    private static readonly (Vector3 Min, Vector3 Max)[] RockBounds = new[]
    {
        // Rock0: X: [-2.58, 3.23], Y: [-0.40, 1.04], Z: [-2.37, 2.42]
        (new Vector3(-2.58f, -0.40f, -2.37f), new Vector3(3.23f, 1.04f, 2.42f)),

        // Rock1: X: [-2.45, 3.87], Y: [-0.35, 1.88], Z: [-2.72, 2.71]
        (new Vector3(-2.45f, -0.35f, -2.72f), new Vector3(3.87f, 1.88f, 2.71f)),

        // Rock2: X: [-2.19, 2.19], Y: [-0.91, 0.91], Z: [-2.75, 2.75]
        (new Vector3(-2.19f, -0.91f, -2.75f), new Vector3(2.19f, 0.91f, 2.75f)),

        // Rock3: X: [-2.19, 2.04], Y: [-0.51, 1.51], Z: [-2.95, 2.91]
        (new Vector3(-2.19f, -0.51f, -2.95f), new Vector3(2.04f, 1.51f, 2.91f))
    };
    
    public Rock(Vector3 position, Vector3 scale, Vector3 rotation, Color color, int rockType = 0) : base(position, scale, rotation)
    {
        IsSolid = true;
        _color = color;
        RockType = rockType;
    }

    public override void Initialize()
    {
        base.Initialize();
        
        var bounds = RockBounds[RockType % RockBounds.Length];
        var (vertices, faces) = BoxColliderFactory.CreateBoxFromBounds(bounds.Min, bounds.Max);
        Collider = new ConvexCollider(this, vertices, faces);
        BoundingSphere = new BoundingSphere(_position, Collider.BoundingRadius);
    }
    

    protected override void DrawMeshes(GraphicsDevice graphicsDevice)
    {
        _effect.Parameters["DiffuseColor"].SetValue(_color.ToVector3());
        base.DrawMeshes(graphicsDevice);
    }
}
