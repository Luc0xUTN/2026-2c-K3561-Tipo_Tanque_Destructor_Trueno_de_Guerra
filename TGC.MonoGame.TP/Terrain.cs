using System.Net.Mime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP;

// Responsabilidad: Representar al terreno donde se va a situar la batalla 
public class Terrain
{
    private Color _color {get; set;}
    
    private VertexBuffer _vertices;
    private IndexBuffer _indices;
    private Effect _effect;
    private Matrix _world;
    
    private string _shaderRoute; 
    
    
    public Terrain(string shaderRoute, Color color)
    {
        _shaderRoute = shaderRoute;
        _color = color;
    }

    public void LoadContent(ContentManager content)
    {
        _effect = content.Load<Effect>(_shaderRoute);
    }
    
    public void Initialize(GraphicsDevice device, float size)
    {
        _world = Matrix.Identity;
        float escala = size / 2; 
        
        var triangeVertices = new[]
        {
            new VertexPosition(new Vector3(1,0,1) * escala),
            new VertexPosition(new Vector3(-1,0,1) * escala),
            new VertexPosition(new Vector3(1,0,-1)  * escala),
            new VertexPosition(new Vector3(-1,0,-1) * escala),
        };
        
        _vertices = new VertexBuffer(device, VertexPosition.VertexDeclaration, triangeVertices.Length, BufferUsage.WriteOnly);
        _vertices.SetData(triangeVertices);

        var indexBuffer = new ushort[]
        {
            0,1,2,
            2,1,3
        };

        _indices = new IndexBuffer(device, IndexElementSize.SixteenBits, indexBuffer.Length, BufferUsage.WriteOnly);
        _indices.SetData(indexBuffer);
    }

    public void Draw(GraphicsDevice device, Matrix view, Matrix projection)
    {
        device.SetVertexBuffer(_vertices);
        device.Indices = _indices;
        
        _effect.Parameters["View"].SetValue(view);
        _effect.Parameters["Projection"].SetValue(projection);
        _effect.Parameters["World"].SetValue(_world);
        _effect.Parameters["DiffuseColor"].SetValue(_color.ToVector3());

        foreach (var pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            
            device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0,0,2);
        }
        
    }
}