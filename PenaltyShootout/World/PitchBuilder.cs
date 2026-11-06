using System;
using Microsoft.Xna.Framework;
using PenaltyShootout.Data;
using PenaltyShootout.Gameplay;
using PenaltyShootout.Rendering;

namespace PenaltyShootout.World;

/// <summary>Trávník s pruhy, čáry hřiště a obě branky se sítí.</summary>
public static class PitchBuilder
{
    public const float HalfWidth = 34f;
    public const float Length = 105f;
    private const float LineY = 0.012f;
    private const float LineW = 0.12f;

    public static void Build(MeshBuilder b, LineBuilder net, ArenaStyle s, Random rnd)
    {
        // okolí hřiště
        b.AddGround(-320, -260, 320, 380, -0.03f, s.Surround);

        // trávník – pruhy přes celé hřiště
        float z = -7f;
        int k = 0;
        while (z < Length + 7f)
        {
            float z1 = MathF.Min(z + 5.25f, Length + 7f);
            b.AddGround(-HalfWidth - 6, z, HalfWidth + 6, z1, 0f, k % 2 == 0 ? s.Grass1 : s.Grass2);
            z = z1;
            k++;
        }

        // v zimě sníh u čar
        if (s.Snow)
        {
            for (int i = 0; i < 260; i++)
            {
                float x = (float)(rnd.NextDouble() * 2 - 1) * (HalfWidth + 5);
                float zz = (float)rnd.NextDouble() * (Length + 12) - 6;
                if (MathF.Abs(x) < HalfWidth - 2 && zz > 1 && zz < Length - 1 && rnd.NextDouble() < 0.7) continue;
                float r = 0.4f + (float)rnd.NextDouble() * 1.4f;
                b.AddDisc(new Vector3(x, 0.006f, zz), r, 7, new Color(236, 240, 246));
            }
        }

        var white = new Color(245, 245, 245);
        b.AddFlatSegment(new(-HalfWidth, 0), new(-HalfWidth, Length), LineW, LineY, white);
        b.AddFlatSegment(new(HalfWidth, 0), new(HalfWidth, Length), LineW, LineY, white);
        b.AddFlatSegment(new(-HalfWidth, Length / 2), new(HalfWidth, Length / 2), LineW, LineY, white);
        b.AddFlatArc(new(0, Length / 2), 9.15f, 0, MathHelper.TwoPi, 48, LineW, LineY, white);
        b.AddDisc(new Vector3(0, LineY, Length / 2), 0.15f, 10, white);

        BuildHalf(b, 0f, 1f, white);
        BuildHalf(b, Length, -1f, white);

        BuildGoal(b, net, 0f, 1f);
        BuildGoal(b, net, Length, -1f);

        // rohové praporky
        foreach (var cz in new[] { 0f, Length })
        foreach (var cx in new[] { -HalfWidth, HalfWidth })
        {
            b.AddCylinder(MeshBuilder.Box(new Vector3(cx, 0.75f, cz), new Vector3(0.04f, 1.5f, 0.04f)), white, 6);
            b.AddBox(MeshBuilder.Box(new Vector3(cx, 1.35f, cz + 0.2f * (cz == 0 ? 1 : -1)), new Vector3(0.02f, 0.3f, 0.4f)),
                new Color(250, 200, 20));
        }
    }

    /// <summary>Čáry u jedné branky. dir = směr do hřiště (+1 / -1).</summary>
    private static void BuildHalf(MeshBuilder b, float zGoal, float dir, Color c)
    {
        Vector2 P(float x, float d) => new(x, zGoal + dir * d);

        b.AddFlatSegment(P(-HalfWidth, 0), P(HalfWidth, 0), LineW, LineY, c);

        // malé vápno
        b.AddFlatSegment(P(-9.16f, 0), P(-9.16f, 5.5f), LineW, LineY, c);
        b.AddFlatSegment(P(9.16f, 0), P(9.16f, 5.5f), LineW, LineY, c);
        b.AddFlatSegment(P(-9.16f, 5.5f), P(9.16f, 5.5f), LineW, LineY, c);

        // velké vápno
        b.AddFlatSegment(P(-20.16f, 0), P(-20.16f, 16.5f), LineW, LineY, c);
        b.AddFlatSegment(P(20.16f, 0), P(20.16f, 16.5f), LineW, LineY, c);
        b.AddFlatSegment(P(-20.16f, 16.5f), P(20.16f, 16.5f), LineW, LineY, c);

        // penaltová značka
        var spot = P(0, ShotResolver.SpotDistance);
        b.AddDisc(new Vector3(spot.X, LineY, spot.Y), 0.13f, 12, c);

        // oblouk před vápnem
        float a = MathF.Asin(5.5f / 9.15f);
        if (dir > 0) b.AddFlatArc(spot, 9.15f, a, MathF.PI - a, 20, LineW, LineY, c);
        else b.AddFlatArc(spot, 9.15f, MathF.PI + a, MathHelper.TwoPi - a, 20, LineW, LineY, c);

        // rohové oblouky
        if (dir > 0)
        {
            b.AddFlatArc(new(HalfWidth, zGoal), 1f, MathF.PI / 2, MathF.PI, 6, LineW, LineY, c);
            b.AddFlatArc(new(-HalfWidth, zGoal), 1f, 0, MathF.PI / 2, 6, LineW, LineY, c);
        }
        else
        {
            b.AddFlatArc(new(HalfWidth, zGoal), 1f, MathF.PI, 1.5f * MathF.PI, 6, LineW, LineY, c);
            b.AddFlatArc(new(-HalfWidth, zGoal), 1f, 1.5f * MathF.PI, MathHelper.TwoPi, 6, LineW, LineY, c);
        }
    }

    /// <summary>Branka: tyče, břevno, zadní konstrukce a síť (čáry).</summary>
    private static void BuildGoal(MeshBuilder b, LineBuilder net, float zGoal, float dir)
    {
        float w = ShotResolver.GoalHalfWidth, h = ShotResolver.GoalHeight, t = ShotResolver.PostThickness;
        var white = new Color(250, 250, 250);
        var grey = new Color(150, 150, 155);

        float postX = w + t / 2;
        b.AddCylinder(MeshBuilder.Box(new Vector3(-postX, (h + t) / 2, zGoal), new Vector3(t, h + t, t)), white, 10);
        b.AddCylinder(MeshBuilder.Box(new Vector3(postX, (h + t) / 2, zGoal), new Vector3(t, h + t, t)), white, 10);
        b.AddCylinder(Matrix.CreateScale(t, 2 * postX + t, t) * Matrix.CreateRotationZ(MathHelper.PiOver2)
                      * Matrix.CreateTranslation(0, h + t / 2, zGoal), white, 10);

        // tvar sítě: nahoře 0.9 m dozadu, dole 2 m dozadu
        float zTop = zGoal - dir * 0.9f;
        float zBottom = zGoal - dir * 2.0f;

        var thin = new Vector3(0.04f);
        void Bar(Vector3 a, Vector3 c)
        {
            var d = c - a;
            float len = d.Length();
            var y = Vector3.Normalize(d);
            var x = Vector3.Normalize(Vector3.Cross(y, MathF.Abs(y.Y) > 0.9f ? Vector3.UnitX : Vector3.UnitY));
            var z = Vector3.Cross(x, y);
            b.AddBox(MeshBuilder.Basis(x * thin.X, y * len, z * thin.Z, (a + c) / 2), grey);
        }
        Bar(new(-postX, h, zTop), new(postX, h, zTop));
        Bar(new(-postX, 0.02f, zBottom), new(postX, 0.02f, zBottom));
        Bar(new(-postX, h, zTop), new(-postX, 0.02f, zBottom));
        Bar(new(postX, h, zTop), new(postX, 0.02f, zBottom));
        Bar(new(-postX, h, zGoal), new(-postX, h, zTop));
        Bar(new(postX, h, zGoal), new(postX, h, zTop));
        Bar(new(-postX, 0.02f, zGoal), new(-postX, 0.02f, zBottom));
        Bar(new(postX, 0.02f, zGoal), new(postX, 0.02f, zBottom));

        var nc = new Color(255, 255, 255);
        const float spacing = 0.16f;
        // střecha, zadní stěna, boky
        net.AddGrid(new(-postX, h, zGoal), new(postX, h, zGoal), new(postX, h, zTop), new(-postX, h, zTop), spacing, nc);
        net.AddGrid(new(-postX, h, zTop), new(postX, h, zTop), new(postX, 0, zBottom), new(-postX, 0, zBottom), spacing, nc);
        net.AddGrid(new(-postX, 0, zGoal), new(-postX, h, zGoal), new(-postX, h, zTop), new(-postX, 0, zBottom), spacing, nc);
        net.AddGrid(new(postX, 0, zGoal), new(postX, h, zGoal), new(postX, h, zTop), new(postX, 0, zBottom), spacing, nc);
    }
}
