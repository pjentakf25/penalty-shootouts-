using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PenaltyShootout.Rendering;

/// <summary>
/// Skládá low-poly geometrii (kvádry, válce, koule, kužely) do jedné sítě.
/// Normály se zadávají explicitně, takže nezáleží na pořadí vrcholů.
/// </summary>
public sealed class MeshBuilder
{
    private readonly List<VertexPositionNormalColor> _v = new();
    private readonly List<int> _i = new();

    public int VertexCount => _v.Count;

    private static readonly (Vector3 N, Vector3 U, Vector3 W)[] BoxFaces =
    {
        (Vector3.UnitX, Vector3.UnitZ, Vector3.UnitY),
        (-Vector3.UnitX, Vector3.UnitZ, Vector3.UnitY),
        (Vector3.UnitY, Vector3.UnitX, Vector3.UnitZ),
        (-Vector3.UnitY, Vector3.UnitX, Vector3.UnitZ),
        (Vector3.UnitZ, Vector3.UnitX, Vector3.UnitY),
        (-Vector3.UnitZ, Vector3.UnitX, Vector3.UnitY),
    };

    /// <summary>Matice z os – kvádr 1×1×1 se roztáhne podél vektorů x, y, z a posune na střed t.</summary>
    public static Matrix Basis(Vector3 x, Vector3 y, Vector3 z, Vector3 t) =>
        new(x.X, x.Y, x.Z, 0, y.X, y.Y, y.Z, 0, z.X, z.Y, z.Z, 0, t.X, t.Y, t.Z, 1);

    public static Matrix Box(Vector3 center, Vector3 size) =>
        Matrix.CreateScale(size) * Matrix.CreateTranslation(center);

    public void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 n, Color col)
    {
        int s = _v.Count;
        _v.Add(new(a, n, col));
        _v.Add(new(b, n, col));
        _v.Add(new(c, n, col));
        _v.Add(new(d, n, col));
        _i.Add(s); _i.Add(s + 1); _i.Add(s + 2);
        _i.Add(s); _i.Add(s + 2); _i.Add(s + 3);
    }

    public void AddTriangle(Vector3 a, Vector3 b, Vector3 c, Vector3 n, Color col)
    {
        int s = _v.Count;
        _v.Add(new(a, n, col));
        _v.Add(new(b, n, col));
        _v.Add(new(c, n, col));
        _i.Add(s); _i.Add(s + 1); _i.Add(s + 2);
    }

    /// <summary>Vodorovný obdélník (tráva, čáry...).</summary>
    public void AddGround(float x0, float z0, float x1, float z1, float y, Color col) =>
        AddQuad(new(x0, y, z0), new(x1, y, z0), new(x1, y, z1), new(x0, y, z1), Vector3.Up, col);

    /// <summary>Plochý pruh mezi dvěma body (x, z) – čáry na hřišti.</summary>
    public void AddFlatSegment(Vector2 p0, Vector2 p1, float width, float y, Color col)
    {
        Vector2 dir = p1 - p0;
        if (dir.LengthSquared() < 1e-6f) return;
        dir.Normalize();
        Vector2 perp = new Vector2(-dir.Y, dir.X) * (width / 2f);
        Vector3 V(Vector2 p) => new(p.X, y, p.Y);
        AddQuad(V(p0 + perp), V(p1 + perp), V(p1 - perp), V(p0 - perp), Vector3.Up, col);
    }

    public void AddFlatArc(Vector2 center, float radius, float a0, float a1, int segments, float width, float y, Color col)
    {
        for (int k = 0; k < segments; k++)
        {
            float t0 = MathHelper.Lerp(a0, a1, k / (float)segments);
            float t1 = MathHelper.Lerp(a0, a1, (k + 1) / (float)segments);
            var p0 = center + new Vector2(MathF.Cos(t0), MathF.Sin(t0)) * radius;
            var p1 = center + new Vector2(MathF.Cos(t1), MathF.Sin(t1)) * radius;
            AddFlatSegment(p0, p1, width, y, col);
        }
    }

    public void AddDisc(Vector3 center, float radius, int segments, Color col)
    {
        for (int k = 0; k < segments; k++)
        {
            float a0 = MathHelper.TwoPi * k / segments;
            float a1 = MathHelper.TwoPi * (k + 1) / segments;
            AddTriangle(center,
                center + new Vector3(MathF.Cos(a0), 0, MathF.Sin(a0)) * radius,
                center + new Vector3(MathF.Cos(a1), 0, MathF.Sin(a1)) * radius,
                Vector3.Up, col);
        }
    }

    /// <summary>Kvádr 1×1×1 se středem v počátku, transformovaný maticí m.</summary>
    public void AddBox(Matrix m, Color col, Color? top = null)
    {
        foreach (var f in BoxFaces)
        {
            Vector3 c = f.N * 0.5f, u = f.U * 0.5f, w = f.W * 0.5f;
            var n = Vector3.Normalize(Vector3.TransformNormal(f.N, m));
            var color = top.HasValue && f.N == Vector3.UnitY ? top.Value : col;
            AddQuad(Vector3.Transform(c - u - w, m), Vector3.Transform(c + u - w, m),
                    Vector3.Transform(c + u + w, m), Vector3.Transform(c - u + w, m), n, color);
        }
    }

    /// <summary>Válec o průměru 1 a výšce 1 (osa Y), transformovaný maticí m.</summary>
    public void AddCylinder(Matrix m, Color col, int segments = 10, bool caps = true)
    {
        Vector3 P(float a, float y) => Vector3.Transform(new Vector3(MathF.Cos(a) * 0.5f, y, MathF.Sin(a) * 0.5f), m);
        for (int k = 0; k < segments; k++)
        {
            float a0 = MathHelper.TwoPi * k / segments;
            float a1 = MathHelper.TwoPi * (k + 1) / segments;
            float am = (a0 + a1) / 2f;
            var n = Vector3.Normalize(Vector3.TransformNormal(new Vector3(MathF.Cos(am), 0, MathF.Sin(am)), m));
            AddQuad(P(a0, -0.5f), P(a1, -0.5f), P(a1, 0.5f), P(a0, 0.5f), n, col);
            if (caps)
            {
                var up = Vector3.Normalize(Vector3.TransformNormal(Vector3.UnitY, m));
                AddTriangle(Vector3.Transform(new Vector3(0, 0.5f, 0), m), P(a0, 0.5f), P(a1, 0.5f), up, col);
                AddTriangle(Vector3.Transform(new Vector3(0, -0.5f, 0), m), P(a0, -0.5f), P(a1, -0.5f), -up, col);
            }
        }
    }

    /// <summary>Kužel (podstava dole, špička nahoře), průměr 1, výška 1.</summary>
    public void AddCone(Matrix m, Color col, int segments = 9)
    {
        var apex = Vector3.Transform(new Vector3(0, 0.5f, 0), m);
        var baseC = Vector3.Transform(new Vector3(0, -0.5f, 0), m);
        Vector3 P(float a) => Vector3.Transform(new Vector3(MathF.Cos(a) * 0.5f, -0.5f, MathF.Sin(a) * 0.5f), m);
        for (int k = 0; k < segments; k++)
        {
            float a0 = MathHelper.TwoPi * k / segments;
            float a1 = MathHelper.TwoPi * (k + 1) / segments;
            float am = (a0 + a1) / 2f;
            var n = Vector3.Normalize(Vector3.TransformNormal(new Vector3(MathF.Cos(am), 0.5f, MathF.Sin(am)), m));
            AddTriangle(P(a0), P(a1), apex, n, col);
            AddTriangle(baseC, P(a0), P(a1), -Vector3.UnitY, col);
        }
    }

    /// <summary>Koule o průměru 1. Barva se určuje pro každou ploška (kvůli šachovnici na míči).</summary>
    public void AddSphere(Matrix m, int rings, int segments, Func<int, int, Color> color)
    {
        Vector3 S(float th, float ph) => new(MathF.Sin(th) * MathF.Cos(ph) * 0.5f, MathF.Cos(th) * 0.5f, MathF.Sin(th) * MathF.Sin(ph) * 0.5f);
        for (int r = 0; r < rings; r++)
        {
            float t0 = MathHelper.Pi * r / rings, t1 = MathHelper.Pi * (r + 1) / rings;
            for (int s = 0; s < segments; s++)
            {
                float p0 = MathHelper.TwoPi * s / segments, p1 = MathHelper.TwoPi * (s + 1) / segments;
                var n = Vector3.Normalize(Vector3.TransformNormal(S((t0 + t1) / 2, (p0 + p1) / 2), m));
                AddQuad(Vector3.Transform(S(t0, p0), m), Vector3.Transform(S(t0, p1), m),
                        Vector3.Transform(S(t1, p1), m), Vector3.Transform(S(t1, p0), m), n, color(r, s));
            }
        }
    }

    public void AddSphere(Matrix m, Color col, int rings = 6, int segments = 8) =>
        AddSphere(m, rings, segments, (_, _) => col);

    public Mesh Build(GraphicsDevice gd) => _v.Count == 0 ? null : new Mesh(gd, _v, _i);
}

public sealed class LineBuilder
{
    private readonly List<VertexPositionColor> _v = new();

    public void AddLine(Vector3 a, Vector3 b, Color c)
    {
        _v.Add(new VertexPositionColor(a, c));
        _v.Add(new VertexPositionColor(b, c));
    }

    /// <summary>Mřížka na čtyřúhelníku p00-p10-p11-p01 (síť brány).</summary>
    public void AddGrid(Vector3 p00, Vector3 p10, Vector3 p11, Vector3 p01, float spacing, Color c)
    {
        int nu = Math.Max(1, (int)MathF.Ceiling(Vector3.Distance(p00, p10) / spacing));
        int nv = Math.Max(1, (int)MathF.Ceiling(Vector3.Distance(p00, p01) / spacing));
        Vector3 Bi(float u, float v) => Vector3.Lerp(Vector3.Lerp(p00, p10, u), Vector3.Lerp(p01, p11, u), v);
        for (int i = 0; i <= nu; i++) AddLine(Bi(i / (float)nu, 0), Bi(i / (float)nu, 1), c);
        for (int j = 0; j <= nv; j++) AddLine(Bi(0, j / (float)nv), Bi(1, j / (float)nv), c);
    }

    public LineMesh Build(GraphicsDevice gd) => _v.Count == 0 ? null : new LineMesh(gd, _v);
}
