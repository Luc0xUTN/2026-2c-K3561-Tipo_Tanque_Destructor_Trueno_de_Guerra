using System;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.scenes;
using TGC.MonoGame.TP.Utils;

namespace TGC.MonoGame.TP.Views;

public class ConfigurationView : View
{
    private View _previousView;
    private GraphicsDevice _device;
    private SpriteFont _font;
    private MenuScene _menuScene;
    private SpriteBatch _spriteBatch;

    private Button _backBtn; 
    
    private Button _musicUpBtn;
    private Button _musicDownBtn;
    
    public ConfigurationView(View previousView, GraphicsDevice device, MenuScene menuScene, SpriteFont font, SpriteBatch spriteBatch)
    {
        _device = device;
        _previousView = previousView;
        _font = font;
        _menuScene = menuScene;
        _spriteBatch = spriteBatch;
    }
    
    public void Initialize()
    {
        int centerX = _device.Viewport.Width / 2;
        int centerY = _device.Viewport.Height / 2;
        
        _backBtn = new Button(_font, "< Atras", new Vector2(0,0), false);
        _backBtn.Click += _menuScene.GetSetView(_previousView);

        Vector2 basePosition = new Vector2(centerX, centerY - 100);
        int numberEntries = 2;
        var labelsPosition = UI.GenerateElementsPosition(basePosition, 0, 100, numberEntries);
        
        var musicPositions =  UI.GenerateElementsPosition(labelsPosition[0], 50, 0, 3);
        _musicUpBtn = new Button(_font, "+",  musicPositions[0], true);
        // _musicUpBtn.Click += GameSettings.GetMusicAdder(0.1f);
        
        _musicDownBtn = new  Button(_font, "-", musicPositions[2], true);
        // _musicDownBtn.Click += GameSettings.GetMusicAdder(0.1f);
    }

    public void Update()
    {
        _backBtn.Update();
    }

    public void Draw()
    {
        _spriteBatch.Begin();
        
        _backBtn.Draw(_spriteBatch);
        
        _spriteBatch.End();
    }
}