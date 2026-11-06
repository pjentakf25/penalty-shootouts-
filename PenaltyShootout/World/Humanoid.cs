using System;
using Microsoft.Xna.Framework;
using PenaltyShootout.Core;
using PenaltyShootout.Data;
using PenaltyShootout.Rendering;

namespace PenaltyShootout.World;

/// <summary>Barvy postavičky (dres, trenýrky, štulpny, pleť, vlasy).</summary>
public sealed class Appearance
{
    private static readonly Color[] SkinTones =
    {
        new(255, 222, 190), new(240, 196, 150), new(222, 170, 120),
        new(196, 136, 90), new(150, 98, 60), new(105, 68, 45),
    };

    private static readonly Color[] HairColors =
    {
        new(25, 20, 18), new(60, 40, 25), new(110, 75, 40), new(200, 160, 90), new(40, 30, 25),
    };

    public Color Shirt, Shorts, Socks, Skin, Hair, Boots, Gloves;
    public bool IsKeeper;
    public bool Bald;

    private static Appearance FromName(string name)
    {
        int h = Rng.StableHash(name);
        return new Appearance
        {
            Skin = SkinTones[h % SkinTones.Length],
            Hair = HairColors[(h / 7) % HairColors.Length],
            Bald = (h / 53) % 9 == 0,
            Boots = (h / 11 % 3) switch { 0 => new Color(20, 20, 22), 1 => new Color(240, 240, 240), _ => new Color(255, 120, 20) },
        };
    }

    public static Appearance ForKicker(Kicker k, Team t)
    {
        var a = FromName(k.Name);
        a.Shirt = t.Shirt;
        a.Shorts = t.Shorts;
        a.Socks = t.Socks;
        return a;
    }

    public static Appearance ForKeeper(Goalkeeper g, Team t)
    {
        var a = FromName(g.Name);
        a.IsKeeper = true;
        a.Shirt = t.KeeperShirt;
        a.Shorts = Color.Lerp(t.KeeperShirt, Color.Black, 0.6f);
        a.Socks = t.KeeperShirt;
        a.Gloves = new Color(245, 245, 245);
        return a;
    }
}

/// <summary>
/// Kostičková postavička (styl low-poly mobilních fotbálků).
/// Lokální osa +Z = dopředu, chodidla v y = 0, kyčle ve výšce 0.9 m.
/// </summary>
public sealed class Humanoid
{
    private const float HipHeight = 0.9f;

    public Appearance Look { get; }

    public Vector3 Position;
    public float Yaw;      // otočení kolem svislé osy
    public float Roll;     // náklon do strany (skok brankáře)
    public float Pitch;    // předklon
    public float Crouch;   // podřep

    public float LegL, LegR;     // švih nohou dopředu (rad)
    public float ArmL, ArmR;     // švih paží dopředu
    public float RaiseL, RaiseR; // zvednutí paží do strany (π = nad hlavu)

    public Humanoid(Appearance look) => Look = look;

    /// <summary>Kořenová transformace (rotace kolem kyčlí).</summary>
    public Matrix Root =>
        Matrix.CreateTranslation(0, -HipHeight, 0)
        * Matrix.CreateRotationX(Pitch)
        * Matrix.CreateRotationZ(Roll)
        * Matrix.CreateTranslation(0, HipHeight - Crouch, 0)
        * Matrix.CreateRotationY(Yaw)
        * Matrix.CreateTranslation(Position);

    public void ResetPose()
    {
        Roll = Pitch = Crouch = 0;
        LegL = LegR = ArmL = ArmR = 0;
        RaiseL = RaiseR = 0.12f;
    }

    public void Draw(Renderer3D r)
    {
        var root = Root;
        var L = Look;

        // nohy (levá = +X, pravá = -X)
        DrawLeg(r, root, 0.11f, LegL);
        DrawLeg(r, root, -0.11f, LegR);

        // pánev, trup
        r.DrawBox(Part(new Vector3(0, 0.93f, 0), new Vector3(0.42f, 0.22f, 0.25f), root), L.Shorts);
        r.DrawBox(Part(new Vector3(0, 1.3f, 0), new Vector3(0.48f, 0.6f, 0.27f), root), L.Shirt);

        // hlava (velká – kreslený styl)
        r.DrawBox(Part(new Vector3(0, 1.62f, 0), new Vector3(0.14f, 0.08f, 0.14f), root), L.Skin);
        r.DrawBox(Part(new Vector3(0, 1.82f, 0), new Vector3(0.34f, 0.34f, 0.32f), root), L.Skin);
        if (!L.Bald)
        {
            r.DrawBox(Part(new Vector3(0, 1.98f, -0.01f), new Vector3(0.36f, 0.08f, 0.35f), root), L.Hair);
            r.DrawBox(Part(new Vector3(0, 1.86f, -0.165f), new Vector3(0.36f, 0.26f, 0.05f), root), L.Hair);
        }
        var eye = new Color(30, 30, 35);
        r.DrawBox(Part(new Vector3(0.075f, 1.84f, 0.162f), new Vector3(0.05f, 0.06f, 0.01f), root), eye);
        r.DrawBox(Part(new Vector3(-0.075f, 1.84f, 0.162f), new Vector3(0.05f, 0.06f, 0.01f), root), eye);

        // paže
        DrawArm(r, root, 1f, ArmL, RaiseL);
        DrawArm(r, root, -1f, ArmR, RaiseR);
    }

    private static Matrix Part(Vector3 center, Vector3 size, Matrix parent) =>
        Matrix.CreateScale(size) * Matrix.CreateTranslation(center) * parent;

    private void DrawLeg(Renderer3D r, Matrix root, float x, float swing)
    {
        var pivot = Matrix.CreateRotationX(-swing) * Matrix.CreateTranslation(x, HipHeight, 0) * root;
        r.DrawBox(Part(new Vector3(0, -0.21f, 0), new Vector3(0.19f, 0.42f, 0.21f), pivot), Look.Shorts);
        r.DrawBox(Part(new Vector3(0, -0.64f, 0), new Vector3(0.16f, 0.46f, 0.17f), pivot), Look.Socks);
        r.DrawBox(Part(new Vector3(0, -0.86f, 0.05f), new Vector3(0.17f, 0.09f, 0.29f), pivot), Look.Boots);
    }

    private void DrawArm(Renderer3D r, Matrix root, float side, float swing, float raise)
    {
        var pivot = Matrix.CreateRotationX(-swing) * Matrix.CreateRotationZ(side * raise)
                    * Matrix.CreateTranslation(side * 0.31f, 1.54f, 0) * root;
        r.DrawBox(Part(new Vector3(0, -0.16f, 0), new Vector3(0.14f, 0.32f, 0.14f), pivot), Look.Shirt);
        r.DrawBox(Part(new Vector3(0, -0.47f, 0), new Vector3(0.12f, 0.32f, 0.12f), pivot),
            Look.IsKeeper ? Look.Shirt : Look.Skin);
        if (Look.IsKeeper)
            r.DrawBox(Part(new Vector3(0, -0.7f, 0), new Vector3(0.17f, 0.16f, 0.17f), pivot), Look.Gloves);
        else
            r.DrawBox(Part(new Vector3(0, -0.68f, 0), new Vector3(0.11f, 0.1f, 0.11f), pivot), Look.Skin);
    }

    public void DrawShadow(Renderer3D r)
    {
        float lift = MathF.Max(0, Position.Y + 0.6f);
        r.DrawShadow(Position, 0.45f + MathF.Abs(MathF.Sin(Roll)) * 0.5f, 0.35f / (1f + lift * 0.4f));
    }
}
