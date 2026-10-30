
using Microsoft.Xna.Framework;

namespace TGC.MonoGame.TP;

public struct Light
{
    public Vector3 Position { get; set; }
    public Vector3 AmbientColor { get; set; } 
    public Vector3 DiffuseColor { get; set; } 
    public Vector3 SpecularColor { get; set; } 
}