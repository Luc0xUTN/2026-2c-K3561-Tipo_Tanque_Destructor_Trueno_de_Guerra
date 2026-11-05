
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.Collisions;

namespace TGC.MonoGame.TP;

public class Tree : Prop
{
    public Tree(Vector3 position, Vector3 scale, Vector3 rotation, float kAmbient, float kDiffuse, float kSpecular, float shininess) 
        : base(position, scale, rotation, kAmbient, kDiffuse, kSpecular, shininess)
    {
        IsSolid = true;
    }

    // A diferencia de una roca, un árbol se aplasta: no empuja al tanque de
    // vuelta (sin corrección de posición, ver PhysicsSystem.CheckPair), solo
    // lo frena un poco y desaparece. 0.85f es a ojo (quita 15% de la
    // velocidad por árbol cruzado) -- como el frenado es MULTIPLICATIVO,
    // cruzar varios seguidos (campo traviesa, fuera de los senderos) compone
    // y termina siendo notoriamente más lento que ir por un carril limpio,
    // sin necesidad de ningún sistema aparte que sepa "esto es bosque".
    // Ajustar jugando si se siente muy brusco o muy gratis.
    public override bool DestroyedOnTankCollision => true;
    public override float TankCollisionSpeedMultiplier => 0.85f;

    // Menor que el choque contra algo sólido (Tank.DefaultCollisionDamage) a
    // propósito: a Dante le importa que abrirse camino por el bosque en vez
    // de los senderos siga doliendo un poco (si no, es un atajo gratis), pero
    // bastante menos que chocar contra una roca u otro tanque.
    public override int TankCollisionDamage => 1;

    public override void Initialize()
    {
        base.Initialize();
        var (vertices, faces) = BoxColliderFactory.CreateBox(halfWidth: 0.5f, height: 3f);
        Collider = new ConvexCollider(this, vertices, faces);
        
        float maxScale = System.MathF.Max(_scale.X, System.MathF.Max(_scale.Y, _scale.Z));
        var center = _position + Vector3.Up * (1.9f * _scale.Y);
        BoundingSphere = new BoundingSphere(center, 2.8f * maxScale);
    }
    protected override void DrawMeshes(GraphicsDevice graphicsDevice)
    {
        graphicsDevice.BlendState = BlendState.Opaque;
        graphicsDevice.DepthStencilState = DepthStencilState.Default;
        
        base.DrawMeshes(graphicsDevice);
    }
}
