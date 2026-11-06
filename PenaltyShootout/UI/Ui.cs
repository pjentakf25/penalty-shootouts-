using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace PenaltyShootout.UI;

public enum Align { Left, Center, Right }

/// <summary>
/// 2D kreslení UI ve virtuálním rozlišení 1280×720 (škáluje se na velikost okna).
/// Všechny textury (pixel, kruh, prstenec) se generují v kódu.
/// </summary>
public static class Ui
{
    public const int Width = 1280;
    public const int Height = 720;

    public static readonly Color Accent = new(255, 200, 40);
    public static readonly Color PanelColor = new(10, 18, 34, 215);
    public static readonly Color GoalGreen = new(70, 210, 100);
    public static readonly Color MissRed = new(230, 65, 60);
    public static readonly Color Muted = new(160, 170, 185);

    public static SpriteFont Font { get; private set; }
    public static SpriteFont Title { get; private set; }
    public static Texture2D Pixel { get; private set; }
    public static Texture2D Circle { get; private set; }
    public static Texture2D Ring { get; private set; }

    public static float Scale { get; private set; } = 1f;
    public static Vector2 Offset { get; private set; }

    public static Matrix Transform =>
        Matrix.CreateScale(Scale, Scale, 1) * Matrix.CreateTranslation(Offset.X, Offset.Y, 0);

    public static void Load(GraphicsDevice gd, ContentManager content)
    {
        Font = content.Load<SpriteFont>("Fonts/Ui");
        Title = content.Load<SpriteFont>("Fonts/Title");

        Pixel = new Texture2D(gd, 1, 1);
        Pixel.SetData(new[] { Color.White });
        Circle = MakeCircle(gd, 128, 0);
        Ring = MakeCircle(gd, 128, 10);
    }

    private static Texture2D MakeCircle(GraphicsDevice gd, int size, float thickness)
    {
        var tex = new Texture2D(gd, size, size);
        var data = new Color[size * size];
        float r = size / 2f - 1;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size / 2f));
            float a = MathHelper.Clamp(r - d, 0, 1);
            if (thickness > 0) a *= MathHelper.Clamp(d - (r - thickness), 0, 1);
            data[y * size + x] = Color.White * a;
        }
        tex.SetData(data);
        return tex;
    }

    public static void UpdateViewport(Viewport vp)
    {
        Scale = MathF.Min(vp.Width / (float)Width, vp.Height / (float)Height);
        Offset = new Vector2((vp.Width - Width * Scale) / 2f, (vp.Height - Height * Scale) / 2f);
    }

    public static Vector2 ScreenToVirtual(Vector2 p) => (p - Offset) / Scale;

    public static void Begin(SpriteBatch sb) =>
        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullNone, null, Transform);

    // ------------------------------------------------------------- primitiva

    public static void Rect(SpriteBatch sb, Rectangle r, Color c) => sb.Draw(Pixel, r, c);

    public static void Frame(SpriteBatch sb, Rectangle r, Color c, int t = 2)
    {
        Rect(sb, new Rectangle(r.X, r.Y, r.Width, t), c);
        Rect(sb, new Rectangle(r.X, r.Bottom - t, r.Width, t), c);
        Rect(sb, new Rectangle(r.X, r.Y, t, r.Height), c);
        Rect(sb, new Rectangle(r.Right - t, r.Y, t, r.Height), c);
    }

    public static void Panel(SpriteBatch sb, Rectangle r, Color? fill = null, Color? accent = null)
    {
        Rect(sb, new Rectangle(r.X + 4, r.Y + 5, r.Width, r.Height), Color.Black * 0.3f);
        Rect(sb, r, fill ?? PanelColor);
        Rect(sb, new Rectangle(r.X, r.Y, r.Width, 3), accent ?? Accent);
    }

    /// <summary>Ztmavení celé obrazovky (včetně okrajů mimo 16:9).</summary>
    public static void Fade(SpriteBatch sb, Color c) => Rect(sb, new Rectangle(-4000, -4000, 9000, 9000), c);

    public static void Line(SpriteBatch sb, Vector2 a, Vector2 b, Color c, float thickness = 2f)
    {
        var d = b - a;
        float len = d.Length();
        if (len < 0.5f) return;
        sb.Draw(Pixel, a, null, c, MathF.Atan2(d.Y, d.X), new Vector2(0, 0.5f), new Vector2(len, thickness), SpriteEffects.None, 0);
    }

    public static void Dot(SpriteBatch sb, Vector2 center, float radius, Color c) =>
        sb.Draw(Circle, center, null, c, 0, new Vector2(64), radius / 64f, SpriteEffects.None, 0);

    public static void RingAt(SpriteBatch sb, Vector2 center, float radius, Color c) =>
        sb.Draw(Ring, center, null, c, 0, new Vector2(64), radius / 64f, SpriteEffects.None, 0);

    public static void Bar(SpriteBatch sb, Rectangle r, float value, Color fill)
    {
        Rect(sb, r, Color.Black * 0.45f);
        Rect(sb, new Rectangle(r.X, r.Y, (int)(r.Width * MathHelper.Clamp(value, 0, 1)), r.Height), fill);
    }

    // ------------------------------------------------------------------ text

    public static Vector2 Measure(string s, float scale = 1f, bool title = false) =>
        (title ? Title : Font).MeasureString(s) * scale;

    public static void Text(SpriteBatch sb, string s, Vector2 pos, Color c, float scale = 1f,
        Align align = Align.Left, bool title = false, bool shadow = true)
    {
        var font = title ? Title : Font;
        var size = font.MeasureString(s) * scale;
        var p = align switch
        {
            Align.Center => pos - new Vector2(size.X / 2, 0),
            Align.Right => pos - new Vector2(size.X, 0),
            _ => pos,
        };
        p = new Vector2(MathF.Round(p.X), MathF.Round(p.Y));
        if (shadow)
            sb.DrawString(font, s, p + new Vector2(2, 2) * MathF.Max(1, scale), Color.Black * (c.A / 255f * 0.6f), 0, Vector2.Zero, scale, SpriteEffects.None, 0);
        sb.DrawString(font, s, p, c, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
    }

    /// <summary>Text vycentrovaný na bod se škálováním kolem středu (vyskakující nápisy).</summary>
    public static void TextCentered(SpriteBatch sb, string s, Vector2 center, Color c, float scale, bool title = true)
    {
        var font = title ? Title : Font;
        var size = font.MeasureString(s);
        var origin = size / 2;
        sb.DrawString(font, s, center + new Vector2(4, 5), Color.Black * (c.A / 255f * 0.55f), 0, origin, scale, SpriteEffects.None, 0);
        sb.DrawString(font, s, center, c, 0, origin, scale, SpriteEffects.None, 0);
    }

    // ------------------------------------------------------------------ obloha

    /// <summary>Přechod oblohy přes celé okno (kreslí se bez transformace UI).</summary>
    public static void Sky(SpriteBatch sb, Viewport vp, Color top, Color bottom, bool stars)
    {
        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone);
        const int strips = 48;
        for (int i = 0; i < strips; i++)
        {
            int y0 = vp.Height * i / strips;
            int y1 = vp.Height * (i + 1) / strips;
            sb.Draw(Pixel, new Rectangle(0, y0, vp.Width, y1 - y0 + 1), Color.Lerp(top, bottom, i / (float)(strips - 1)));
        }
        if (stars)
        {
            var rnd = new Random(5);
            for (int i = 0; i < 160; i++)
            {
                var p = new Vector2((float)rnd.NextDouble() * vp.Width, (float)rnd.NextDouble() * vp.Height * 0.6f);
                float s = 0.6f + (float)rnd.NextDouble() * 1.6f;
                sb.Draw(Circle, p, null, Color.White * (0.3f + (float)rnd.NextDouble() * 0.6f), 0, new Vector2(64), s / 64f, SpriteEffects.None, 0);
            }
            sb.Draw(Circle, new Vector2(vp.Width * 0.82f, vp.Height * 0.12f), null, new Color(240, 240, 225), 0, new Vector2(64), 28f / 64f, SpriteEffects.None, 0);
        }
        sb.End();
    }
}
