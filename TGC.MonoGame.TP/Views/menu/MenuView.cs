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
    // private View _settingsView; 
    
    
    private Label _gameTitle;
    private Button _playBtn;
    // private Button _settingsBtn; 
    private Button _howToPlayBtn;
    private Button _exitBtn; 

    private int _lastViewportWidth;
    private int _lastViewportHeight;
    
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
        _gameTitle = new Label(_font, "Tipo Tanque Destructor Trueno de Guerra", Vector2.Zero, true);
        
        _howToPlayView = new HowToPlayView(this, _device, _menuScene, _font, _spriteBatch);
        _howToPlayView.Initialize();
        
        // _settingsView = new ConfigurationView(this, _device, _menuScene, _font, _spriteBatch);
        // _settingsView.Initialize();
        
        _playBtn = new Button(_font, "Jugar", Vector2.Zero, true);
        _playBtn.Click += PlayBtn_handler;
        
        _howToPlayBtn = new Button(_font, "Como jugar?", Vector2.Zero, true);
        _howToPlayBtn.Click += _menuScene.GetSetView(_howToPlayView);
        
        // _settingsBtn = new Button(_font, "Configuracion", Vector2.Zero, true);
        // _settingsBtn.Click += _menuScene.GetSetView(_settingsView);
        
        _exitBtn = new Button(_font, "Salir", Vector2.Zero, true);
        _exitBtn.Click += SalirBtn_handler;

        UpdateLayout();
    }

    public void UpdateLayout()
    {
        int width = _device.Viewport.Width;
        int height = _device.Viewport.Height;
        if (width == _lastViewportWidth && height == _lastViewportHeight)
            return;

        _lastViewportWidth = width;
        _lastViewportHeight = height;

        int centerX = width / 2;
        int centerY = height / 2;

        float titleY = Math.Max(60f, height * 0.22f);
        _gameTitle.Position = new Vector2(centerX, titleY);

        int padding = Math.Clamp((int)(height * 0.10f), 60, 100);
        Vector2 btnsBasePosition = new Vector2(centerX, centerY - padding * 0.5f);
        List<Vector2> positions = UI.GenerateElementsPosition(btnsBasePosition, 0, padding, 3);

        _playBtn.SetPosition(positions[0]);
        _howToPlayBtn.SetPosition(positions[1]);
        _exitBtn.SetPosition(positions[2]);
    }
    
    public void Update()
    {
        UpdateLayout();
        _playBtn.Update();
        _howToPlayBtn.Update();
        // _settingsBtn.Update();
        _exitBtn.Update();
    }

    public void Draw()
    {
        _spriteBatch.Begin();
        
        _gameTitle.Draw(_spriteBatch);
        _playBtn.Draw(_spriteBatch);
        _howToPlayBtn.Draw(_spriteBatch);
        // _settingsBtn.Draw(_spriteBatch);
        _exitBtn.Draw(_spriteBatch);
        
        _spriteBatch.End();
    }

    private void PlayBtn_handler(object sender, EventArgs args)
    {
        _menuScene.FlTransition = true;
    }
    
    private void SalirBtn_handler(object sender, EventArgs args)
    {
        _salirDelJuego.Invoke();   
    }
}