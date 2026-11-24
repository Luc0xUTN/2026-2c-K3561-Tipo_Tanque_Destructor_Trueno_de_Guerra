using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.Cameras;
using TGC.MonoGame.TP.Views.UIElements;

namespace TGC.MonoGame.TP.Views;

// Mismo patrón que MenuView (dibuja con el SpriteBatch que le pasa el
// caller, el mismo que ya usa la mira, dentro del mismo Begin()/End() --
// SpriteBatch no admite un segundo Begin() anidado), pero no implementa la
// interfaz View: a diferencia de las vistas de menú, su Update necesita el
// tiempo restante de partida (lo tiene que decidir GameScene, no el HUD) y
// nadie lo trata polimórficamente como View.
public class GameHudView
{
    private const int Margin = 20;
    private const int BarWidth = 220;
    private const int BarHeight = 24;

    private readonly GraphicsDevice _device;
    private readonly SpriteFont _font;
    private readonly SpriteBatch _spriteBatch;
    private readonly PlayerTank _playerTank;
    private readonly List<AITank> _enemyTanks;
    private readonly List<AITank> _allyTanks;
    private BaseCamera _camera;
    private Matrix _projection;

    private int _initialEnemyCount;
    private int _initialAllyCount;
    private HealthBar _healthBar;
    private HealthBar _reloadBar;
    private Label _enemyCountLabel;
    private Label _allyCountLabel;
    private Label _timerLabel;
    private Label _godModeLabel;
    private Label _resultTitleLabel;
    private Label _resultSubtitleLabel;
    private bool _isGameOver;
    private Texture2D _pixel;

    private int _lastViewportWidth;
    private int _lastViewportHeight;

    public GameHudView(GraphicsDevice device, SpriteFont font, SpriteBatch spriteBatch, Texture2D pixel, PlayerTank playerTank, List<AITank> enemyTanks, List<AITank> allyTanks, BaseCamera camera, Matrix projection)
    {
        _device = device;
        _font = font;
        _spriteBatch = spriteBatch;
        _playerTank = playerTank;
        _enemyTanks = enemyTanks;
        _allyTanks = allyTanks;
        _camera = camera;
        _projection = projection;
        _pixel = pixel;

        _healthBar = new HealthBar(pixel, Vector2.Zero, BarWidth, BarHeight);
        _enemyCountLabel = new Label(_font, "", Vector2.Zero);
        _allyCountLabel = new Label(_font, "", Vector2.Zero);
        _reloadBar = new HealthBar(pixel, Vector2.Zero, BarWidth, 10, Color.DarkSlateGray, Color.Cyan);
        _timerLabel = new Label(_font, "", Vector2.Zero, true);
        _godModeLabel = new Label(_font, "GODMODE", Vector2.Zero) { TextColor = Color.Yellow };
        _resultTitleLabel = new Label(_font, "", Vector2.Zero, true);
        _resultSubtitleLabel = new Label(_font, "", Vector2.Zero, true);

        UpdateLayout();
    }

    public void SetProjection(Matrix projection)
    {
        _projection = projection;
    }

    public void UpdateLayout()
    {
        int width = _device.Viewport.Width;
        int height = _device.Viewport.Height;
        if (width == _lastViewportWidth && height == _lastViewportHeight)
            return;

        _lastViewportWidth = width;
        _lastViewportHeight = height;

        int margin = Math.Clamp((int)(width * 0.015f), 16, 32);
        int barWidth = Math.Clamp((int)(width * 0.16f), 200, 360);
        int barHeight = Math.Clamp((int)(height * 0.03f), 20, 32);

        var barPosition = new Vector2(margin, height - margin - barHeight);
        _healthBar.Position = barPosition;
        _healthBar.Width = barWidth;
        _healthBar.Height = barHeight;

        var lineHeight = _font.LineSpacing + Math.Max(4, (int)(height * 0.006f));
        var labelPosition = new Vector2(margin, barPosition.Y - lineHeight);
        _enemyCountLabel.Position = labelPosition;

        var allyLabelPosition = new Vector2(margin, labelPosition.Y - lineHeight);
        _allyCountLabel.Position = allyLabelPosition;

        int reloadBarHeight = Math.Max(8, (int)(barHeight * 0.42f));
        int reloadBarGap = Math.Max(6, (int)(barHeight * 0.35f));
        var reloadBarPosition = new Vector2(margin, allyLabelPosition.Y - reloadBarHeight - reloadBarGap);
        _reloadBar.Position = reloadBarPosition;
        _reloadBar.Width = barWidth;
        _reloadBar.Height = reloadBarHeight;

        var timerPosition = new Vector2(width / 2f, margin + _font.LineSpacing / 2f);
        _timerLabel.Position = timerPosition;

        var godModePosition = new Vector2(margin, margin);
        _godModeLabel.Position = godModePosition;

        var center = new Vector2(width / 2f, height / 2f);
        _resultTitleLabel.Position = center - new Vector2(0, 40);
        _resultSubtitleLabel.Position = center + new Vector2(0, 20);
    }

    public void Initialize()
    {
        _initialEnemyCount = _enemyTanks.Count;
        _initialAllyCount = _allyTanks.Count;
    }

    public void Update(float matchTimeRemaining)
    {
        UpdateLayout();
        _healthBar.Ratio = _playerTank.HealthRatio;
        _enemyCountLabel.Text = $"Enemigos: {_enemyTanks.Count}/{_initialEnemyCount}";
        _allyCountLabel.Text = $"Aliados: {_allyTanks.Count}/{_initialAllyCount}";
        _reloadBar.Ratio = _playerTank.FireCooldownRatio;

        var minutes = (int)(matchTimeRemaining / 60f);
        var seconds = (int)(matchTimeRemaining % 60f);
        _timerLabel.Text = $"{minutes:D2}:{seconds:D2}";
    }

    // La llama GameScene una sola vez, cuando se cumple alguna condición de
    // fin de partida (victoria/derrota/tregua) -- de ahí en más Draw() la
    // muestra encima del HUD normal, que sigue dibujando los últimos valores
    // (congelados, GameScene deja de llamar Update sobre los tanques).
    public void ShowEndMessage(string title, string subtitle)
    {
        _isGameOver = true;
        _resultTitleLabel.Text = title;
        _resultSubtitleLabel.Text = subtitle;
    }

    public void Draw()
    {
        _spriteBatch.Begin();

        _healthBar.Draw(_spriteBatch);
        _enemyCountLabel.Draw(_spriteBatch);
        _allyCountLabel.Draw(_spriteBatch);
        _timerLabel.Draw(_spriteBatch);

        // Aparece/desaparece entre disparos: solo visible mientras recarga.
        if (_reloadBar.Ratio < 1f)
            _reloadBar.Draw(_spriteBatch);

        if (_playerTank.IsGodMode)
            _godModeLabel.Draw(_spriteBatch);

        if (_isGameOver)
        {
            _resultTitleLabel.Draw(_spriteBatch);
            _resultSubtitleLabel.Draw(_spriteBatch);
        }
        else
        {
            this.DrawReticle();
        }

        _spriteBatch.End();
    }
    
    // Mira : la cruz se proyecta desde la dirección que marca el mouse, así que
    // sigue al mouse en pantalla y la cámara la recentra a medida que se acomoda detrás del tanque.
    // La torreta y el cañón persiguen esa misma dirección, más lento todavía.
    private void DrawReticle()
    {
        const int gap = 5;
        const int arm = 14;
        const int thickness = 2;
        int half = thickness / 2;
        Color color = new Color(0.25f, 1f, 0.25f, 0.9f);

        // Punto lejano en la dirección de puntería, saliendo de la torreta.
        Vector3 aimPoint = _playerTank.GetPosition() + Vector3.Up * 2.5f + _camera.AimDirection * 200f;

        Vector3 screen = _device.Viewport.Project(aimPoint, _projection, _camera.GetView(), Matrix.Identity);
        if (screen.Z < 0f || screen.Z > 1f)
            return; // quedó detrás de la cámara

        // Se mantiene siempre dentro de la pantalla, aun apuntando al límite del rango del cañón.
        int margin = gap + arm;
        int cx = (int)MathHelper.Clamp(screen.X, margin, _device.Viewport.Width - margin);
        int cy = (int)MathHelper.Clamp(screen.Y, margin, _device.Viewport.Height - margin);

        // Brazos horizontales.
        _spriteBatch.Draw(_pixel, new Rectangle(cx - gap - arm, cy - half, arm, thickness), color);
        _spriteBatch.Draw(_pixel, new Rectangle(cx + gap, cy - half, arm, thickness), color);
        // Brazos verticales.
        _spriteBatch.Draw(_pixel, new Rectangle(cx - half, cy - gap - arm, thickness, arm), color);
        _spriteBatch.Draw(_pixel, new Rectangle(cx - half, cy + gap, thickness, arm), color);
        // Punto central.
        _spriteBatch.Draw(_pixel, new Rectangle(cx - 1, cy - 1, 2, 2), color);
    }

    
}
