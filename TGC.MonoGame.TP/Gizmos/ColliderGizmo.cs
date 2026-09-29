using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using TGC.MonoGame.TP.Collisions;

public class ColliderGizmo : IDebugDrawable
{
    private bool _drawNormals = false;
    private Color _color = Color.Blue;

    private readonly ConvexCollider _collider;

    private readonly BasicEffect _effect;

    public ColliderGizmo(GraphicsDevice graphicsDevice, ConvexCollider collider, bool drawNormals = false)
    {
        _collider = collider;

        _effect = new BasicEffect(graphicsDevice)
        {
            VertexColorEnabled = true
        };

        _drawNormals = drawNormals;
    }


    public void DrawDebug(GraphicsDevice graphicsDevice, Matrix view, Matrix projection)
    {
        var vertices = new List<VertexPositionColor>();

        // Edges
        foreach (var edge in _collider.GetEdges())
        {
            Vector3 a = _collider.Vertices[edge.A];
            Vector3 b = _collider.Vertices[edge.B];

            a = Vector3.Transform(a, _collider.World);
            b = Vector3.Transform(b, _collider.World);

            vertices.Add(new VertexPositionColor(a, _color));
            vertices.Add(new VertexPositionColor(b, _color));
        }

        // Normales
        if (_drawNormals)
        {    
            foreach (var face in _collider.Faces)
            {
                Vector3 start = Vector3.Transform(face.Center, _collider.World);
                Vector3 normal = Vector3.TransformNormal(
                    face.Normal,
                    _collider.World
                );

                normal.Normalize();

                Vector3 end = start + normal;

                vertices.Add(new VertexPositionColor(start, _color));
                vertices.Add(new VertexPositionColor(end, _color));
            }
        }

        _effect.World = Matrix.Identity;
        _effect.View = view;
        _effect.Projection = projection;

        foreach (var pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();

            graphicsDevice.DrawUserPrimitives(
                PrimitiveType.LineList,
                vertices.ToArray(),
                0,
                vertices.Count / 2
            );
        }
    }
}