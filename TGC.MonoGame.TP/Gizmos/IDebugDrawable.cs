using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public interface IDebugDrawable
{
    void DrawDebug(GraphicsDevice graphicsDevice, Matrix view, Matrix projection);
}