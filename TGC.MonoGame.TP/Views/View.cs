using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP.Views;

public interface View
{
    void Initialize();
    void Update();
    void Draw();
}