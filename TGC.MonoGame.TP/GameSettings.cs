using System;
using System.Runtime.CompilerServices;

namespace TGC.MonoGame.TP;

// Responsabilidad: Singleton que contiene todas las carácticas de configuración del juego para que cualquier clase lo pueda obtener. 
public static class GameSettings
{
    public const float MapSize  = 300f;
    // Radianes de puntería por píquel de movimiento del mouse. Bajar este valor hace la mira
    // menos sensible (antes 0.005).
    public static float Sensitivity { get; set; } = 0.0025f;
    public static bool DebugMode { get; set;  }= false;

    // Si está en false, un proyectil ignora a los tanques de su mismo equipo
    // (jugador vs enemigos). Default true (los disparos son agnósticos a quién
    // los tiró) -- queda como variable de código por ahora, listo para exponerse
    // desde un menú de opciones más adelante.
    public static bool FriendlyFireEnabled { get; set; } = true;
    
    public static float MusicVolumen { get; set; } = 0.0f;
    public static float SoundEffectsVolumen { get; set; } = 0.0f;

    public static EventHandler GetMusicAdder(float value)
    {
        return (obj, args) =>
        {
            MusicVolumen += value;
        };
    }
    
}