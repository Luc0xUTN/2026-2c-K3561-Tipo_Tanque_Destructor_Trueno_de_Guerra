namespace TGC.MonoGame.TP;

// Responsabilidad: Singleton que contiene todas las carácticas de configuración del juego para que cualquier clase lo pueda obtener. 
public static class GameSettings
{
    public const float MapSize  = 300f;
    public static float Sensitivity { get; set; } = 0.005f;
    public static bool DebugMode { get; set;  }= false;

    // Si está en false, un proyectil ignora a los tanques de su mismo equipo
    // (jugador vs enemigos). Default true (los disparos son agnósticos a quién
    // los tiró) -- queda como variable de código por ahora, listo para exponerse
    // desde un menú de opciones más adelante.
    public static bool FriendlyFireEnabled { get; set; } = true;
}