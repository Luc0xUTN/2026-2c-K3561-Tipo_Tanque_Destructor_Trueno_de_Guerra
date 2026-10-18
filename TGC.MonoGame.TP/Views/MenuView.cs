using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.scenes;
using TGC.MonoGame.TP.Views.UIElements;

namespace TGC.MonoGame.TP.Views;

public class MenuView : View
{
    private GraphicsDevice _device;
    
    private SpriteFont _font;
    
    private MenuScene _menuScene;
    
    private SpriteBatch _spriteBatch;


    private Label _gameTitle;
    private Button _playBtn;
    
    public MenuView(GraphicsDevice device, MenuScene menuScene, SpriteFont font, SpriteBatch spriteBatch)
    {
        _device = device;
        _menuScene = menuScene;
        _font = font;
        _spriteBatch = spriteBatch;
    }

    public void Initialize()
    {
        int centerX =  _device.Viewport.Width / 2;
        int centerY = _device.Viewport.Height / 2;
        
        _gameTitle = new Label(_font,"Tipo Tanque Destructor Trueno de Guerra", new Vector2(centerX, 50), true);
        
        _playBtn = new Button(_font, "Jugar", new Vector2(centerX, centerY), true);
        _playBtn.Click += PlayBtn_handler;
    }
    
    
    public void Update()
    {
        _playBtn.Update();
    }

    public void Draw()
    {
        _spriteBatch.Begin();
        
        _gameTitle.Draw(_spriteBatch);
        _playBtn.Draw(_spriteBatch);
        
        _spriteBatch.End();
    }

    private void PlayBtn_handler(object sender, EventArgs args)
    {
        _menuScene.FlTransition = true;
    }
}