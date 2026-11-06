using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PenaltyShootout.Data;

namespace PenaltyShootout.Rendering;

public sealed class Camera3D
{
    public Vector3 Position = new(0, 1.8f, 16.4f);
    public Vector3 Target = new(0, 1.15f, 0);
    public float FieldOfView = MathHelper.ToRadians(48);

    public Matrix View => Matrix.CreateLookAt(Position, Target, Vector3.Up);

    public Matrix Projection(Viewport vp) =>
        Matrix.CreatePerspectiveFieldOfView(FieldOfView, vp.AspectRatio, 0.1f, 900f);
}

/// <summary>Vykreslování 3D scény přes BasicEffect (osvětlení, mlha, stíny).</summary>
public sealed class Renderer3D
{
    private readonly GraphicsDevice _gd;
    private readonly Mesh _unitBox;
    private readonly Mesh _shadowDisc;
    private Matrix _view, _projection;

    public BasicEffect Lit { get; }
    public BasicEffect Unlit { get; }
    public BasicEffect Lines { get; }
    public Camera3D Camera { get; } = new();
    public GraphicsDevice Device => _gd;

    public Renderer3D(GraphicsDevice gd)
    {
        _gd = gd;
        Lit = new BasicEffect(gd) { VertexColorEnabled = true, LightingEnabled = true, PreferPerPixelLighting = true };
        Unlit = new BasicEffect(gd) { VertexColorEnabled = true };
        Lines = new BasicEffect(gd) { VertexColorEnabled = true };

        var b = new MeshBuilder();
        b.AddBox(Matrix.Identity, Color.White);
        _unitBox = b.Build(gd);

        var d = new MeshBuilder();
        d.AddDisc(Vector3.Zero, 1f, 18, Color.Black);
        _shadowDisc = d.Build(gd);
    }

    public void ApplyStyle(ArenaStyle s)
    {
        Lit.AmbientLightColor = s.Ambient;
        Lit.SpecularColor = Vector3.Zero;
        Lit.DirectionalLight0.Enabled = true;
        Lit.DirectionalLight0.Direction = s.LightDirection;
        Lit.DirectionalLight0.DiffuseColor = s.LightColor;
        Lit.DirectionalLight0.SpecularColor = Vector3.Zero;
        Lit.DirectionalLight1.Enabled = true;
        Lit.DirectionalLight1.Direction = Vector3.Normalize(new Vector3(0.4f, -0.3f, 0.8f));
        Lit.DirectionalLight1.DiffuseColor = s.FillColor;
        Lit.DirectionalLight1.SpecularColor = Vector3.Zero;
        Lit.DirectionalLight2.Enabled = false;

        foreach (var e in new[] { Lit, Unlit, Lines })
        {
            e.FogEnabled = true;
            e.FogColor = s.FogColor.ToVector3();
            e.FogStart = s.FogStart;
            e.FogEnd = s.FogEnd;
        }
    }

    public void Begin()
    {
        _gd.DepthStencilState = DepthStencilState.Default;
        _gd.BlendState = BlendState.Opaque;
        _gd.RasterizerState = RasterizerState.CullNone;

        _view = Camera.View;
        _projection = Camera.Projection(_gd.Viewport);
        foreach (var e in new[] { Lit, Unlit, Lines })
        {
            e.View = _view;
            e.Projection = _projection;
        }
    }

    public void Draw(Mesh mesh, Matrix world, bool lit = true)
    {
        if (mesh == null) return;
        var e = lit ? Lit : Unlit;
        e.World = world;
        e.DiffuseColor = Vector3.One;
        e.Alpha = 1f;
        mesh.Draw(_gd, e);
    }

    /// <summary>Kvádr 1×1×1 transformovaný maticí, obarvený barvou (postavičky).</summary>
    public void DrawBox(Matrix world, Color color)
    {
        Lit.World = world;
        Lit.DiffuseColor = color.ToVector3();
        Lit.Alpha = 1f;
        _unitBox.Draw(_gd, Lit);
        Lit.DiffuseColor = Vector3.One;
    }

    /// <summary>Kulatý stín na trávě (průhledný).</summary>
    public void DrawShadow(Vector3 position, float radius, float alpha)
    {
        _gd.BlendState = BlendState.AlphaBlend;
        _gd.DepthStencilState = DepthStencilState.DepthRead;
        Unlit.World = Matrix.CreateScale(radius, 1, radius) * Matrix.CreateTranslation(position.X, 0.02f, position.Z);
        Unlit.DiffuseColor = Vector3.One;
        Unlit.Alpha = alpha;
        _shadowDisc.Draw(_gd, Unlit);
        Unlit.Alpha = 1f;
        _gd.BlendState = BlendState.Opaque;
        _gd.DepthStencilState = DepthStencilState.Default;
    }

    public void DrawLines(LineMesh lines, float alpha)
    {
        if (lines == null) return;
        _gd.BlendState = BlendState.AlphaBlend;
        _gd.DepthStencilState = DepthStencilState.DepthRead;
        Lines.World = Matrix.Identity;
        Lines.Alpha = alpha;
        lines.Draw(_gd, Lines);
        _gd.BlendState = BlendState.Opaque;
        _gd.DepthStencilState = DepthStencilState.Default;
    }

    /// <summary>Převod bodu ve světě na pixel na obrazovce.</summary>
    public Vector2 Project(Vector3 world)
    {
        var p = _gd.Viewport.Project(world, _projection, _view, Matrix.Identity);
        return new Vector2(p.X, p.Y);
    }
}
