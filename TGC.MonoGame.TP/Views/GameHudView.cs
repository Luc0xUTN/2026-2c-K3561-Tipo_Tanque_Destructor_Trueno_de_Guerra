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

        var barPosition = new Vector2(Margin, _device.Viewport.Height - Margin - BarHeight);
        _healthBar = new HealthBar(pixel, barPosition, BarWidth, BarHeight);

        // Apilados hacia arriba desde la barra de vida, separados por el alto
        // real de una línea de este font (_font.LineSpacing) más un margen --
        // un número fijo (30) quedaba más chico que el texto renderizado y
        // las dos etiquetas se superponían.
        var lineHeight = _font.LineSpacing + 6;
        var labelPosition = new Vector2(Margin, barPosition.Y - lineHeight);
        _enemyCountLabel = new Label(_font, "", labelPosition);

        var allyLabelPosition = new Vector2(Margin, labelPosition.Y - lineHeight);
        _allyCountLabel = new Label(_font, "", allyLabelPosition);

        // Barra de reload, apilada arriba de todo lo anterior -- no es texto,
        // así que el gap no depende de LineSpacing, un margen fijo chico alcanza.
        const int reloadBarHeight = 10;
        const int reloadBarGap = 10;
        var reloadBarPosition = new Vector2(Margin, allyLabelPosition.Y - reloadBarHeight - reloadBarGap);
        _reloadBar = new HealthBar(pixel, reloadBarPosition, BarWidth, reloadBarHeight, Color.DarkSlateGray, Color.Cyan);

        // Timer, centrado arriba -- la X se recalcula en base al viewport
        // (Width/2), no es una posición de pantalla fija.
        // IsCentered centra el texto en los DOS ejes alrededor de Position (no
        // solo el horizontal) -- con Y = Margin directamente, la mitad de
        // arriba del texto quedaba dibujada por encima del borde de pantalla
        // y se recortaba. Sumando medio LineSpacing, Position.Y pasa a ser el
        // centro vertical del texto, y su borde superior cae justo en Margin.
        var timerPosition = new Vector2(_device.Viewport.Width / 2f, Margin + _font.LineSpacing / 2f);
        _timerLabel = new Label(_font, "", timerPosition, true);

        var godModePosition = new Vector2(Margin, Margin);
        _godModeLabel = new Label(_font, "GODMODE", godModePosition) { TextColor = Color.Yellow };

        var center = new Vector2(_device.Viewport.Width / 2f, _device.Viewport.Height / 2f);
        _resultTitleLabel = new Label(_font, "", center - new Vector2(0, 40), true);
        _resultSubtitleLabel = new Label(_font, "", center + new Vector2(0, 20), true);
    }

    public void Initialize()
    {
        _initialEnemyCount = _enemyTanks.Count;
        _initialAllyCount = _allyTanks.Count;
    }

    public void Update(float matchTimeRemaining)
    {
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
