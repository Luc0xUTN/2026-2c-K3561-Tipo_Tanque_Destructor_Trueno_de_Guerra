using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
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

    private int _lastViewportWidth;
    private int _lastViewportHeight;
    
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
        _backBtn = new Button(_font, "< Atras", Vector2.Zero, false);
        _backBtn.Click += _menuScene.GetSetView(_previousView);

        _musicUpBtn = new Button(_font, "+", Vector2.Zero, true);
        _musicDownBtn = new Button(_font, "-", Vector2.Zero, true);

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

        int margin = Math.Clamp((int)(width * 0.025f), 20, 50);
        _backBtn.SetPosition(new Vector2(margin, margin));

        int centerX = width / 2;
        int centerY = height / 2;

        Vector2 basePosition = new Vector2(centerX, centerY - 100);
        int numberEntries = 2;
        var labelsPosition = UI.GenerateElementsPosition(basePosition, 0, 100, numberEntries);
        
        var musicPositions = UI.GenerateElementsPosition(labelsPosition[0], 50, 0, 3);
        _musicUpBtn.SetPosition(musicPositions[0]);
        _musicDownBtn.SetPosition(musicPositions[2]);
    }

    public void Update()
    {
        UpdateLayout();
        _backBtn.Update();
    }

    public void Draw()
    {
        _spriteBatch.Begin();
        
        _backBtn.Draw(_spriteBatch);
        
        _spriteBatch.End();
    }
}