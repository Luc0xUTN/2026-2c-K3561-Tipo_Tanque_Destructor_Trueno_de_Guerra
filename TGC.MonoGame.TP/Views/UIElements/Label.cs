using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP.Views.UIElements;

public class Label
{
    public string Text { get; set; }
    public Vector2 Position { get; set; }
    public Color TextColor { get; set; }
        
    public bool IsCentered { get; set; }
    public bool HasPulseAnimation { get; set; }

    private SpriteFont _font;
    private float _currentScale = 1.0f;
    private float _totalTime = 0f;

    public Label(SpriteFont font, string text, Vector2 position, bool isCentered = false)
    {
        _font = font;
        Text = text;
        Position = position;
        IsCentered = isCentered;
        TextColor = Color.White;
        HasPulseAnimation = false;
    }

    public void Update(GameTime gameTime)
    {
        if (HasPulseAnimation)
        {
            _totalTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
            _currentScale = 1.0f + (float)Math.Sin(_totalTime * 10f) * 0.2f;
        }
        else
        {
            _currentScale = 1.0f;
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        Vector2 origin = Vector2.Zero;

        if (IsCentered)
        {
            Vector2 size = _font.MeasureString(Text);
            origin = new Vector2(size.X / 2f, size.Y / 2f); 
        }

        spriteBatch.DrawString(
            _font, 
            Text, 
            Position, 
            TextColor, 
            0f,         
            origin,     
            _currentScale, 
            SpriteEffects.None, 
            0f
        );
    }
}