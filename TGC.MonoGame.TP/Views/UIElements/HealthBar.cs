using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP.Views.UIElements;

// Misma técnica que la mira de GameScene: una textura de 1x1 estirada vía
// Rectangle. Dos rectángulos superpuestos, mismo alto y posición, el de
// relleno con ancho proporcional a Ratio (0..1) sobre el de fondo.
public class HealthBar
{
    private readonly Texture2D _pixel;

    public Vector2 Position { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public float Ratio { get; set; } = 1f;

    public HealthBar(Texture2D pixel, Vector2 position, int width, int height)
    {
        _pixel = pixel;
        Position = position;
        Width = width;
        Height = height;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        var x = (int)Position.X;
        var y = (int)Position.Y;
        var ratio = MathHelper.Clamp(Ratio, 0f, 1f);

        var background = new Rectangle(x, y, Width, Height);
        spriteBatch.Draw(_pixel, background, Color.DarkRed);

        var fillWidth = (int)(Width * ratio);
        if (fillWidth > 0)
        {
            var fill = new Rectangle(x, y, fillWidth, Height);
            spriteBatch.Draw(_pixel, fill, Color.LimeGreen);
        }
    }
}
