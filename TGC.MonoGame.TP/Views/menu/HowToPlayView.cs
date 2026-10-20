using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.scenes;
using TGC.MonoGame.TP.Utils;
using TGC.MonoGame.TP.Views.UIElements;

namespace TGC.MonoGame.TP.Views;

public class HowToPlayView : View
{
    private View _previousView;
    private GraphicsDevice _device;
    private SpriteFont _font;
    private MenuScene _menuScene;
    private SpriteBatch _spriteBatch;

    private List<Label> _labels = new List<Label>();

    private List<string> _labelsStr = new List<string>
    {
        "Avanzar hacia adeltante presionar tecla W", 
        "Retrocer presionar tecla S", 
        "Girar hacia la derecha presionar tecla D", 
        "Girar hacia la izquierda presionar tecla E",
        "Mover la camara utilizar le mouse",
        "Mover la camara sin la torreta presionar tecla C"
    };

    private Button _backBtn; 
    
    public HowToPlayView(View previousView, GraphicsDevice device, MenuScene menuScene, SpriteFont font, SpriteBatch spriteBatch)
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

        Vector2 basePosition = new Vector2(centerX, centerY - 250);
        int padding = 100;
        int numberElements = 6;

        var position = UI.GenerateElementsPosition(basePosition, 0, padding, numberElements);
        position.Reverse(); 
        
        var positionsStack = new Stack<Vector2>(position);
        
        _labelsStr.ForEach(ls => _labels.Add(new Label(_font, ls, positionsStack.Pop(), true)));

        _backBtn = new Button(_font, "< Atras", new Vector2(0,0), false);
        _backBtn.Click += _menuScene.GetSetView(_previousView);
    }

    public void Update()
    {
        _backBtn.Update();
    }

    public void Draw()
    {
        _spriteBatch.Begin();
        
        _labels.ForEach(l => l.Draw(_spriteBatch));
        _backBtn.Draw(_spriteBatch);
        
        _spriteBatch.End();
    }
}