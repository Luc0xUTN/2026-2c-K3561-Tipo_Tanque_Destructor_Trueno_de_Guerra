namespace TGC.MonoGame.TP;

// Reemplaza el chequeo "es PlayerTank o no" de Projectile.SameTeam, que solo
// podía distinguir jugador-vs-resto. Con esto cualquier par de tanques (los
// dos aliados, los dos enemigos, aliado-jugador, etc.) se compara igual.
public enum Team
{
    Allied,
    Enemy
}
