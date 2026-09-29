using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP;


// Responsabilidad: Representar a una entidad que se dibuja 
// Si bien el comportamiento compartido que existe entre props y tanks es poco, por temas de legibilidad se puede abstraer a una clase
// En el futuro, cuando implementemos texturas, la lógica de cargar una textura a una entidad debería ubicarse acá 
public abstract class Entity
{
    protected Vector3 _position; 
    protected Vector3 _scale;
    protected Vector3 _rotation;
    protected Color _color; 
    
    protected Matrix _world;

    protected Model  _model;
    protected Effect _effect;

    public Vector3 GetPosition() => _position;
    public Vector3 GetScale() => _scale;
    public Vector3 GetRotation() => _rotation;
    public Matrix GetWorld() => _world;
    
    public Entity(Vector3 position, Vector3 scale, Vector3 rotation, Color color)
    {
        _position = position;
        _scale = scale; 
        _rotation = rotation;
        _color = color;
    }

    public void Initialize()
    {
        SetWorldMatrix();
    }
    
    
    public void LoadContent(Model model,  Effect effect)
    {
        _model = model;
        _effect = effect;
        
        foreach (var mesh in _model.Meshes)
        {
            foreach (var meshPart in mesh.MeshParts)
            {
                meshPart.Effect = _effect;
            }
        }
    }
    
    public void SetWorldMatrix()
    {
        Quaternion qrotation = Quaternion.CreateFromYawPitchRoll(_rotation.X, _rotation.Y, _rotation.Z);
        _world =  Matrix.CreateScale(_scale) * Matrix.CreateFromQuaternion(qrotation) * Matrix.CreateTranslation(_position);
    }

    public void Draw(GraphicsDevice graphicsDevice, Matrix view, Matrix projection)
    {
        _effect.Parameters["Projection"].SetValue(projection);
        _effect.Parameters["View"].SetValue(view);
        _effect.Parameters["DiffuseColor"].SetValue(_color.ToVector3());
        
        this.DrawMeshes();
    }

    protected abstract void DrawMeshes();
}