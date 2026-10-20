using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.Cameras;
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
    private BaseCamera _camera;
    private Matrix _projection;
    
    private int _initialEnemyCount;
    private HealthBar _healthBar;
    private Label _enemyCountLabel;
    private Texture2D _pixel;
    
    
    public GameHudView(GraphicsDevice device, SpriteFont font, SpriteBatch spriteBatch, Texture2D pixel, Tank playerTank, List<EnemyTank> enemyTanks, BaseCamera camera, Matrix projection)
    {
        _device = device;
        _font = font;
        _spriteBatch = spriteBatch;
        _playerTank = playerTank;
        _enemyTanks = enemyTanks;
        _camera = camera;
        _projection = projection;
        _pixel = pixel;
        
        var barPosition = new Vector2(Margin, _device.Viewport.Height - Margin - BarHeight);
        _healthBar = new HealthBar(pixel, barPosition, BarWidth, BarHeight);

        var labelPosition = new Vector2(Margin, barPosition.Y - 50);
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
        _spriteBatch.Begin();
        
        _healthBar.Draw(_spriteBatch);
        _enemyCountLabel.Draw(_spriteBatch);
        
        this.DrawReticle();
        
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
