namespace TGC.MonoGame.TP;

// Responsabilidad: Singleton que contiene todas las carácticas de configuración del juego para que cualquier clase lo pueda obtener. 
public static class GameSettings
{
    public const float MapSize  = 300f;
    public static float Sensitivity { get; set; } = 0.005f;
    public static bool DebugMode { get; set;  }= false; 
}