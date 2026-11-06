using System;

namespace PenaltyShootout.Core;

/// <summary>Sdílený generátor náhodných čísel s pomocnými funkcemi.</summary>
public static class Rng
{
    public static Random Shared { get; } = new();

    public static float Value => Shared.NextSingle();

    public static float Range(float min, float max) => min + (max - min) * Shared.NextSingle();

    public static bool Chance(float p) => Shared.NextSingle() < p;

    public static float Sign => Shared.Next(2) == 0 ? -1f : 1f;

    /// <summary>Normální rozdělení (Box–Muller), střed 0, odchylka 1.</summary>
    public static float Gauss()
    {
        float u1 = 1f - Shared.NextSingle();
        float u2 = Shared.NextSingle();
        return MathF.Sqrt(-2f * MathF.Log(u1)) * MathF.Cos(2f * MathF.PI * u2);
    }

    /// <summary>Stabilní hash textu (string.GetHashCode je v .NET náhodný pro každý běh).</summary>
    public static int StableHash(string s)
    {
        unchecked
        {
            int h = 23;
            foreach (char c in s) h = h * 31 + c;
            return h & 0x7fffffff;
        }
    }
}
