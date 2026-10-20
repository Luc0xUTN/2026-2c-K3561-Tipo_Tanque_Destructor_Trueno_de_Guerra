using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

public class Button
    {
        private SpriteFont _font;
        private string _text;

        private Texture2D _icon;

        private Rectangle _bounds; 
        private Color _normalColor = Color.White;
        private Color _hoverColor = Color.Yellow; 
        private Color _currentColor;

        private MouseState _previousMouseState;
        private bool _isPressed;

        public bool IsCentered { get; private set; }
        public event EventHandler Click;

        public Button(SpriteFont font, string text, Vector2 position, bool isCentered = false)
        {
            _font = font;
            _text = text;
            IsCentered = isCentered;
            
            Vector2 size = _font.MeasureString(text);
            CalculateBounds(position, size);
            _previousMouseState = Mouse.GetState();
        }

        public Button(Texture2D icon, Vector2 position, float scale = 1.0f, bool isCentered = false)
        {
            _icon = icon;
            IsCentered = isCentered;
            
            Vector2 size = new Vector2(icon.Width * scale, icon.Height * scale);
            CalculateBounds(position, size);
            _previousMouseState = Mouse.GetState();
        }

        private void CalculateBounds(Vector2 position, Vector2 size)
        {
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
                    _isPressed = true;
                }

                if (_isPressed && 
                    currentMouseState.LeftButton == ButtonState.Released && 
                    _previousMouseState.LeftButton == ButtonState.Pressed)
                {
                    _isPressed = false;
                    Click?.Invoke(this, EventArgs.Empty);
                }
            }
            else
            {
                _currentColor = _normalColor;
                if (currentMouseState.LeftButton == ButtonState.Released)
                {
                    _isPressed = false;
                }
            }

            _previousMouseState = currentMouseState;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (_icon != null)
            {
                spriteBatch.Draw(_icon, _bounds, _currentColor);
            }
            else if (_font != null && !string.IsNullOrEmpty(_text))
            {
                spriteBatch.DrawString(_font, _text, new Vector2(_bounds.X, _bounds.Y), _currentColor);
            }
        }
    }