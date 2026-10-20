using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.scenes;
using TGC.MonoGame.TP.Utils;
using TGC.MonoGame.TP.Views.UIElements;

namespace TGC.MonoGame.TP.Views;

public class MenuView : View
{
    private GraphicsDevice _device;
    
    private SpriteFont _font;
    
    private MenuScene _menuScene;
    
    private SpriteBatch _spriteBatch;

    private Action _salirDelJuego;

    private View _howToPlayView;
    
    
    private Label _gameTitle;
    private Button _playBtn;
    private Button _settingsBtn; 
    private Button _howToPlayBtn;
    private Button _exitBtn; 
    
    public MenuView(GraphicsDevice device, MenuScene menuScene, SpriteFont font, SpriteBatch spriteBatch, Action salirDelJuego)
    {
        _device = device;
        _menuScene = menuScene;
        _font = font;
        _spriteBatch = spriteBatch;
        _salirDelJuego = salirDelJuego;
    }

    public void Initialize()
    {
        int centerX =  _device.Viewport.Width / 2;
        int centerY = _device.Viewport.Height / 2;
        
        _gameTitle = new Label(_font,"Tipo Tanque Destructor Trueno de Guerra", new Vector2(centerX, 200), true);

        _howToPlayView = new HowToPlayView(this, _device, _menuScene, _font, _spriteBatch);
        _howToPlayView.Initialize();
        
        Vector2 btnsBasePosition = new Vector2(centerX, centerY - 50); 
        const int padding = 100;
        const int numberOfElements = 4;
        
        List<Vector2> positions = UI.GenerateElementsPosition(btnsBasePosition, 0, padding, numberOfElements); 
        
        _playBtn = new Button(_font, "Jugar", positions[0], true);
        _playBtn.Click += PlayBtn_handler;
        
        _howToPlayBtn = new Button(_font, "Como jugar?", positions[1], true);
        _howToPlayBtn.Click += _menuScene.GetSetView(_howToPlayView);
        
        _settingsBtn = new Button(_font, "Configuracion", positions[2], true);
        _settingsBtn.Click += SettingsBtn_handler;
        
        _exitBtn = new Button(_font, "Salir", positions[3], true);
        _exitBtn.Click += SalirBtn_handler; 
    }
    
    
    public void Update()
    {
        _playBtn.Update();
        _howToPlayBtn.Update();
        _settingsBtn.Update();
        _exitBtn.Update();
    }

    public void Draw()
    {
        _spriteBatch.Begin();
        
        _gameTitle.Draw(_spriteBatch);
        _playBtn.Draw(_spriteBatch);
        _howToPlayBtn.Draw(_spriteBatch);
        _settingsBtn.Draw(_spriteBatch);
        _exitBtn.Draw(_spriteBatch);
        
        _spriteBatch.End();
    }

    private void PlayBtn_handler(object sender, EventArgs args)
    {
        _menuScene.FlTransition = true;
    }
    
    private void SettingsBtn_handler(object sender, EventArgs args)
    {
        
    }

    private void SalirBtn_handler(object sender, EventArgs args)
    {
        _salirDelJuego.Invoke();   
    }
}