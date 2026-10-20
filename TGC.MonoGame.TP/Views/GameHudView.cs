using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.Views.UIElements;

namespace TGC.MonoGame.TP.Views;

// Mismo patrón que MenuView: una View que vive dentro de su escena (acá
// GameScene), dibuja con el SpriteBatch que le pasa el caller (el mismo que
// ya usa la mira, dentro del mismo Begin()/End() -- SpriteBatch no admite
// un segundo Begin() anidado).
public class GameHudView : View
{
    private const int Margin = 20;
    private const int BarWidth = 220;
    private const int BarHeight = 24;

    private readonly GraphicsDevice _device;
    private readonly SpriteFont _font;
    private readonly SpriteBatch _spriteBatch;
    private readonly Tank _playerTank;
    private readonly List<EnemyTank> _enemyTanks;

    private int _initialEnemyCount;
    private HealthBar _healthBar;
    private Label _enemyCountLabel;

    public GameHudView(GraphicsDevice device, SpriteFont font, SpriteBatch spriteBatch, Texture2D pixel, Tank playerTank, List<EnemyTank> enemyTanks)
    {
        _device = device;
        _font = font;
        _spriteBatch = spriteBatch;
        _playerTank = playerTank;
        _enemyTanks = enemyTanks;

        var barPosition = new Vector2(Margin, _device.Viewport.Height - Margin - BarHeight);
        _healthBar = new HealthBar(pixel, barPosition, BarWidth, BarHeight);

        var labelPosition = new Vector2(Margin, barPosition.Y - 28);
        _enemyCountLabel = new Label(_font, "", labelPosition);
    }

    public void Initialize()
    {
        _initialEnemyCount = _enemyTanks.Count;
    }

    public void Update()
    {
        _healthBar.Ratio = _playerTank.HealthRatio;
        _enemyCountLabel.Text = $"Enemigos: {_enemyTanks.Count}/{_initialEnemyCount}";
    }

    public void Draw()
    {
        _healthBar.Draw(_spriteBatch);
        _enemyCountLabel.Draw(_spriteBatch);
    }
}
