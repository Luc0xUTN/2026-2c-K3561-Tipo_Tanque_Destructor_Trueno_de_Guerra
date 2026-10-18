using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace TGC.MonoGame.TP.Views.Elements;

public class Button
{
    private SpriteFont _font;
    private string _text;
    private Rectangle _bounds; // Caja de colisión
        
    private Color _normalColor = Color.White;
    private Color _hoverColor = Color.Yellow;
    private Color _currentColor;

    private MouseState _previousMouseState;

    public event EventHandler Click;

    public Button(SpriteFont font, string text, Vector2 position)
    {
        _font = font;
        _text = text;
            
        Vector2 size = _font.MeasureString(text);
        _bounds = new Rectangle((int)position.X, (int)position.Y, (int)size.X, (int)size.Y);
            
        _currentColor = _normalColor;
    }

    public void Update()
    {
        MouseState currentMouseState = Mouse.GetState();
        Point mousePosition = new Point(currentMouseState.X, currentMouseState.Y);

        if (_bounds.Contains(mousePosition))
        {
            _currentColor = _hoverColor;

            if (currentMouseState.LeftButton == ButtonState.Pressed && 
                _previousMouseState.LeftButton == ButtonState.Released)
            {
                Click?.Invoke(this, EventArgs.Empty);
            }
        }
        else
        {
            _currentColor = _normalColor;
        }

        _previousMouseState = currentMouseState;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.DrawString(_font, _text, new Vector2(_bounds.X, _bounds.Y), _currentColor);
    }
}