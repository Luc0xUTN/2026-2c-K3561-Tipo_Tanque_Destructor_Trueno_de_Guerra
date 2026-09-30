using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


using TGC.MonoGame.TP;

public static class DebugManager
{
    private static readonly List<IDebugDrawable> _drawables = new();

    public static void Add(IDebugDrawable drawable)
    {
        _drawables.Add(drawable);
    }

    public static void Draw(GraphicsDevice graphicsDevice, Matrix view, Matrix projection)
    {
        foreach (var drawable in _drawables)
        {
            drawable.DrawDebug(
                graphicsDevice,
                view,
                projection
            );
        }
    }
}