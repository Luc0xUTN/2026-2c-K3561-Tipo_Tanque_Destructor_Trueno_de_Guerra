using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

public class Button
    {
        private SpriteFont _font;
        private string _text;
        private Rectangle _bounds; 
        
        private Color _normalColor = Color.White;
        private Color _hoverColor = Color.Yellow;
        private Color _currentColor;

        private MouseState _previousMouseState;

        public bool IsCentered { get; private set; }

        public event EventHandler Click;

        public Button(SpriteFont font, string text, Vector2 position, bool isCentered = false)
        {
            _font = font;
            _text = text;
            IsCentered = isCentered;
            
            Vector2 size = _font.MeasureString(text);

            int x = (int)position.X;
            int y = (int)position.Y;

            if (IsCentered)
            {
                x -= (int)(size.X / 2f);
                y -= (int)(size.Y / 2f);
            }

            _bounds = new Rectangle(x, y, (int)size.X, (int)size.Y);
            
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