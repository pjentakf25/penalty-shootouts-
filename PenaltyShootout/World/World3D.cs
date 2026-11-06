using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PenaltyShootout.Data;
using PenaltyShootout.Rendering;

namespace PenaltyShootout.World;

/// <summary>Celé statické prostředí (hřiště + okolí) sestavené do pár velkých sítí.</summary>
public sealed class World3D : IDisposable
{
    private readonly GraphicsDevice _gd;
    private Mesh _lit, _glow;
    private LineMesh _net;

    public ArenaStyle Style { get; private set; }
    public Snowfall Snow { get; } = new();

    public World3D(GraphicsDevice gd) => _gd = gd;

    public void Build(Arena arena, Color homeColor, Color awayColor)
    {
        Dispose();
        Style = ArenaStyle.For(arena);
        var rnd = new Random(1234);

        var lit = new MeshBuilder();
        var glow = new MeshBuilder();
        var net = new LineBuilder();

        PitchBuilder.Build(lit, net, Style, rnd);
        if (Style.HasStands)
        {
            SurroundingsBuilder.BuildStadium(lit, glow, Style, rnd, homeColor, awayColor);
            if (Style.Clouds) SurroundingsBuilder.BuildClouds(glow, rnd, PitchBuilder.Length);
        }
        else
        {
            SurroundingsBuilder.BuildPark(lit, glow, Style, rnd, homeColor, awayColor);
        }

        _lit = lit.Build(_gd);
        _glow = glow.Build(_gd);
        _net = net.Build(_gd);
    }

    public void DrawOpaque(Renderer3D r)
    {
        r.Draw(_lit, Matrix.Identity);
        r.Draw(_glow, Matrix.Identity, lit: false);
    }

    public void DrawTransparent(Renderer3D r) => r.DrawLines(_net, 0.55f);

    public void Dispose()
    {
        _lit?.Dispose();
        _glow?.Dispose();
        _net?.Dispose();
        _lit = _glow = null;
        _net = null;
    }
}
