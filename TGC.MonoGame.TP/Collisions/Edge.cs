using System;

namespace TGC.MonoGame.TP.Collisions;

public readonly struct Edge : IEquatable<Edge>
{
    public int A { get; }
    public int B { get; }

    public Edge(int a, int b)
    {
        A = Math.Min(a, b);
        B = Math.Max(a, b);
    }

    public bool Equals(Edge other)
    {
        return A == other.A && B == other.B;
    }

    public override bool Equals(object obj)
    {
        return obj is Edge other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(A, B);
    }
}