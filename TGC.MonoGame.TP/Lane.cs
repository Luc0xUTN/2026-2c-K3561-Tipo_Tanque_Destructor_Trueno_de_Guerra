using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace TGC.MonoGame.TP;

// Responsabilidad: Generar la curva central de un carril entre dos puntos ancla
// (Midpoint Displacement, ver repo de contexto progress/07-lane-generation.md para
// la derivación completa), y poder consultar la distancia de un punto a esa curva.
// Un carril NUNCA se dibuja — es un dato geométrico que Forest usa para saber dónde
// NO poner props (ver progress/07, sección "pipeline corregido").
public static class Lane
{
    // arcoBase: sesgo perpendicular DETERMINÍSTICO (no aleatorio), como fracción del
    // largo del segmento — es lo que separa a los 3 carriles entre sí desde el
    // principio. A diferencia de la primera versión, este sesgo NO es un paso previo
    // separado con su propia recursión: se aplica únicamente en el primer nivel de
    // la ÚNICA jerarquía recursiva de Midpoint Displacement (sumado al ruido normal
    // de ese nivel), y de ahí en más toda la curva sale de la misma recursión sin
    // costuras. Aplicarlo como paso previo (versión anterior) generaba un "codo"
    // artificial en el punto del arco, porque las dos mitades quedaban resueltas por
    // recursiones independientes sin continuidad entre sí — no es lo que hace un
    // Midpoint Displacement real.
    public static List<Vector2> GenerateCurve(Vector2 start, Vector2 end, int niveles, float desviacionRelativa, float arcoBase, Random random)
    {
        var points = Displace(start, end, niveles, desviacionRelativa, arcoBase, random, esPrimerNivel: true);
        points.Add(end);
        return points;
    }

    private static List<Vector2> Displace(Vector2 a, Vector2 b, int niveles, float f, float arcoBase, Random random, bool esPrimerNivel)
    {
        if (niveles <= 0)
            return new List<Vector2> { a };

        var mid = (a + b) * 0.5f;
        var dir = b - a;
        var length = dir.Length();

        // Perpendicular en 2D: intercambiar componentes y negar una (rotación de 90°).
        var perp = new Vector2(-dir.Y, dir.X) / length;

        var maxDisplacement = f * length;
        var ruido = ((float)random.NextDouble() * 2f - 1f) * maxDisplacement;
        var sesgo = esPrimerNivel ? arcoBase * length : 0f;
        var displaced = mid + perp * (sesgo + ruido);

        // Orden fijo (izquierda, después derecha) en cada nivel: es lo que garantiza
        // que la misma seed reproduzca siempre la misma curva. El sesgo determinístico
        // solo se aplica una vez, en la raíz de la recursión — todos los niveles
        // siguientes son puro ruido orgánico.
        var left = Displace(a, displaced, niveles - 1, f, arcoBase, random, false);
        var right = Displace(displaced, b, niveles - 1, f, arcoBase, random, false);
        left.AddRange(right);
        return left;
    }

    // Distancia de un punto al segmento más cercano de la polilínea — usado para
    // saber si un punto cae "sobre" el carril (dentro de su ancho navegable).
    public static float DistanceToPolyline(Vector2 point, List<Vector2> polyline)
    {
        var minDistance = float.MaxValue;
        for (var i = 0; i < polyline.Count - 1; i++)
        {
            var distance = DistanceToSegment(point, polyline[i], polyline[i + 1]);
            if (distance < minDistance)
                minDistance = distance;
        }
        return minDistance;
    }

    private static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        var lengthSquared = ab.LengthSquared();
        if (lengthSquared < 1e-6f)
            return Vector2.Distance(point, a);

        var t = MathHelper.Clamp(Vector2.Dot(point - a, ab) / lengthSquared, 0f, 1f);
        var closest = a + ab * t;
        return Vector2.Distance(point, closest);
    }
}
