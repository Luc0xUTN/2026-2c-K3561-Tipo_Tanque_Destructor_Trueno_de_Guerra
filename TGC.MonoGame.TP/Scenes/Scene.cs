using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP.scenes;

public interface Scene
{
    void Initialize();
    void LoadContent();
    void Update(float elapsedTime);
    void Draw();
    void Unload();
    bool ShouldTransition();
    Scene Transition(); 
}