using System;
using Microsoft.Xna.Framework;
using PenaltyShootout.Data;
using PenaltyShootout.Rendering;

namespace PenaltyShootout.World;

/// <summary>Okolí hřiště: stadion s tribunami a fanoušky, nebo park se stromy a klubovnou.</summary>
public static class SurroundingsBuilder
{
    private static readonly Color[] AdColors =
    {
        new(20, 90, 200), new(240, 150, 20), new(235, 235, 235), new(200, 30, 40), new(30, 160, 80), new(120, 40, 170),
    };

    // ------------------------------------------------------------------ stadion

    public static void BuildStadium(MeshBuilder lit, MeshBuilder glow, ArenaStyle s, Random rnd, Color homeColor, Color awayColor)
    {
        float L = PitchBuilder.Length;
        var palette = new[] { homeColor, homeColor, awayColor, awayColor, Color.White,
            new Color(30, 30, 35), new Color(200, 40, 40), new Color(40, 80, 180), new Color(240, 200, 30) };

        // tribuny: za brankami a po stranách
        BuildStand(lit, rnd, palette, new Vector3(0, 0, -9f), Vector3.UnitX, -Vector3.UnitZ, 86f, 18, 0.05f);
        BuildStand(lit, rnd, palette, new Vector3(0, 0, L + 9f), Vector3.UnitX, Vector3.UnitZ, 86f, 18, 0.05f);
        BuildStand(lit, rnd, palette, new Vector3(-43f, 0, L / 2), Vector3.UnitZ, -Vector3.UnitX, 122f, 18, 0.05f);
        BuildStand(lit, rnd, palette, new Vector3(43f, 0, L / 2), Vector3.UnitZ, Vector3.UnitX, 122f, 18, 0.05f);

        // rohové bloky (vyplní mezery mezi tribunami)
        var concrete = new Color(120, 122, 130);
        foreach (var cx in new[] { -1f, 1f })
        foreach (var cz in new[] { -1f, 1f })
        {
            var c = new Vector3(cx * 50f, 6f, cz < 0 ? -16f : L + 16f);
            lit.AddBox(MeshBuilder.Box(c, new Vector3(14f, 12f, 14f)), concrete);
        }

        // reklamní panely (v noci svítí)
        var ads = s.Night ? glow : lit;
        AdRow(ads, new Vector3(-30, 0, -3.5f), Vector3.UnitX, 60f);
        AdRow(ads, new Vector3(-30, 0, L + 3.5f), Vector3.UnitX, 60f);
        AdRow(ads, new Vector3(-37f, 0, 0), Vector3.UnitZ, L);
        AdRow(ads, new Vector3(37f, 0, 0), Vector3.UnitZ, L);

        // stožáry osvětlení
        foreach (var cx in new[] { -1f, 1f })
        foreach (var cz in new[] { -14f, L + 14f })
        {
            var basePos = new Vector3(cx * 52f, 0, cz);
            lit.AddCylinder(MeshBuilder.Box(basePos + new Vector3(0, 21, 0), new Vector3(1.0f, 42, 1.0f)), new Color(90, 92, 100), 8);
            float yaw = MathF.Atan2(-basePos.X, L / 2 - basePos.Z);
            var lamp = Matrix.CreateScale(8f, 4f, 0.6f) * Matrix.CreateRotationX(0.45f) * Matrix.CreateRotationY(yaw)
                       * Matrix.CreateTranslation(basePos + new Vector3(0, 43, 0));
            lit.AddBox(Matrix.CreateScale(8.6f, 4.6f, 0.5f) * Matrix.CreateTranslation(0, 0, -0.4f) * Matrix.CreateRotationX(0.45f)
                       * Matrix.CreateRotationY(yaw) * Matrix.CreateTranslation(basePos + new Vector3(0, 43, 0)), new Color(60, 62, 70));
            (s.Night ? glow : lit).AddBox(lamp, s.Night ? new Color(255, 252, 235) : new Color(215, 220, 225));
        }

        // velká obrazovka za protější brankou
        lit.AddBox(MeshBuilder.Box(new Vector3(0, 19, L + 26), new Vector3(22, 9, 1)), new Color(40, 40, 45));
        (s.Night ? glow : lit).AddBox(MeshBuilder.Box(new Vector3(0, 19, L + 25.4f), new Vector3(20, 7.5f, 0.3f)),
            s.Night ? new Color(40, 120, 220) : new Color(30, 70, 140));
    }

    private static void AdRow(MeshBuilder b, Vector3 start, Vector3 along, float length)
    {
        int seg = (int)(length / 6f);
        var outward = Vector3.Cross(Vector3.Up, along);
        for (int i = 0; i < seg; i++)
        {
            var center = start + along * (i * 6f + 3f) + Vector3.Up * 0.45f;
            b.AddBox(MeshBuilder.Basis(along * 5.9f, Vector3.Up * 0.9f, outward * 0.12f, center), AdColors[i % AdColors.Length]);
        }
    }

    /// <summary>
    /// Stupňovitá tribuna. origin = střed přední hrany, along = směr podél tribuny,
    /// outward = směr od hřiště. Na každém stupni sedí fanoušci (barevné kostičky).
    /// </summary>
    private static void BuildStand(MeshBuilder b, Random rnd, Color[] palette, Vector3 origin, Vector3 along, Vector3 outward,
        float length, int rows, float emptyChance)
    {
        const float rowDepth = 0.85f, rowRise = 0.45f, baseH = 1.6f;
        var wall = new Color(70, 72, 80);
        var step1 = new Color(150, 152, 160);
        var step2 = new Color(135, 137, 146);
        var up = Vector3.Up;

        b.AddBox(MeshBuilder.Basis(along * length, up * baseH, outward * 0.4f, origin + outward * 0.2f + up * baseH / 2), wall);

        for (int r = 0; r < rows; r++)
        {
            float h = baseH + r * rowRise;
            var c = origin + outward * (0.4f + r * rowDepth + rowDepth / 2) + up * (h / 2);
            b.AddBox(MeshBuilder.Basis(along * length, up * h, outward * rowDepth, c), r % 2 == 0 ? step1 : step2);

            int n = (int)(length / 0.62f);
            for (int k = 0; k < n; k++)
            {
                if (rnd.NextDouble() < emptyChance) continue;
                float t = -length / 2 + (k + 0.5f) * length / n + (float)(rnd.NextDouble() - 0.5) * 0.12f;
                float ph = 0.5f + (float)rnd.NextDouble() * 0.18f;
                var p = origin + along * t + outward * (0.4f + r * rowDepth + 0.32f) + up * (h + ph / 2);
                var shirt = palette[rnd.Next(palette.Length)];
                var skin = Color.Lerp(new Color(250, 215, 180), new Color(110, 72, 48), (float)rnd.NextDouble() * 0.8f);
                b.AddBox(MeshBuilder.Basis(along * 0.42f, up * ph, outward * 0.28f, p), shirt, skin);
            }
        }

        float top = baseH + rows * rowRise;
        float depth = 0.4f + rows * rowDepth;
        b.AddBox(MeshBuilder.Basis(along * length, up * (top + 3.5f), outward * 0.6f, origin + outward * (depth + 0.3f) + up * (top + 3.5f) / 2), wall);

        // střecha + sloupy
        float roofDepth = rows * rowDepth * 0.8f;
        float roofY = top + 5.5f;
        b.AddBox(MeshBuilder.Basis(along * (length + 1), up * 0.5f, outward * roofDepth, origin + outward * (depth - roofDepth / 2 + 0.6f) + up * roofY),
            new Color(55, 58, 66));
        for (float t = -length / 2; t <= length / 2 + 0.1f; t += 18f)
        {
            var p = origin + along * t + outward * (depth + 0.2f) + up * (roofY / 2);
            b.AddBox(MeshBuilder.Basis(along * 0.5f, up * roofY, outward * 0.5f, p), new Color(85, 88, 96));
        }
    }

    // ------------------------------------------------------------------ park / zima

    public static void BuildPark(MeshBuilder lit, MeshBuilder glow, ArenaStyle s, Random rnd, Color homeColor, Color awayColor)
    {
        float L = PitchBuilder.Length;
        bool winter = s.Snow;
        var snow = new Color(240, 244, 250);

        // kopce v dálce
        for (int i = 0; i < 16; i++)
        {
            float a = MathHelper.TwoPi * i / 16f + (float)rnd.NextDouble() * 0.3f;
            float dist = 230 + (float)rnd.NextDouble() * 60;
            var c = new Vector3(MathF.Cos(a) * dist, -6, L / 2 + MathF.Sin(a) * dist);
            var col = winter ? Color.Lerp(snow, new Color(190, 200, 215), (float)rnd.NextDouble() * 0.5f)
                             : Color.Lerp(new Color(70, 130, 60), new Color(95, 150, 75), (float)rnd.NextDouble());
            lit.AddSphere(Matrix.CreateScale(120 + (float)rnd.NextDouble() * 60, 40 + (float)rnd.NextDouble() * 30, 110) * Matrix.CreateTranslation(c), col, 6, 10);
        }

        // stromy kolem hřiště
        int placed = 0, guard = 0;
        while (placed < 170 && guard++ < 3000)
        {
            float x = (float)(rnd.NextDouble() * 2 - 1) * 130f;
            float z = -95f + (float)rnd.NextDouble() * (L + 190f);
            if (MathF.Abs(x) < 46 && z > -14 && z < L + 14) continue;
            if (x < -45 && x > -72 && z > 25 && z < 55) continue; // místo pro klubovnu
            Tree(lit, rnd, new Vector3(x, 0, z), winter);
            placed++;
        }

        // plot kolem hřiště
        var fence = winter ? new Color(70, 75, 85) : new Color(40, 95, 60);
        FenceLine(lit, new Vector2(-41, -8), new Vector2(41, -8), fence);
        FenceLine(lit, new Vector2(-41, L + 8), new Vector2(41, L + 8), fence);
        FenceLine(lit, new Vector2(-41, -8), new Vector2(-41, L + 8), fence);
        FenceLine(lit, new Vector2(41, -8), new Vector2(41, L + 8), fence);

        // klubovna
        var wall = new Color(215, 190, 150);
        var roof = winter ? snow : new Color(170, 60, 45);
        var house = new Vector3(-58, 0, 40);
        lit.AddBox(MeshBuilder.Box(house + new Vector3(0, 2.2f, 0), new Vector3(10, 4.4f, 14)), wall);
        Roof(lit, house + new Vector3(0, 4.4f, 0), 11f, 15f, 2.6f, roof);
        lit.AddBox(MeshBuilder.Box(house + new Vector3(5.02f, 1.1f, 0), new Vector3(0.1f, 2.2f, 1.2f)), new Color(110, 70, 40));
        for (int i = -1; i <= 1; i += 2)
            (s.Night ? glow : lit).AddBox(MeshBuilder.Box(house + new Vector3(5.02f, 2.6f, i * 4f), new Vector3(0.1f, 1.2f, 2f)), new Color(150, 200, 235));

        // malá dřevěná tribunka s pár diváky
        var palette = new[] { homeColor, awayColor, new Color(60, 60, 70), new Color(200, 60, 40), Color.White };
        var benchWood = new Color(150, 110, 70);
        for (int r = 0; r < 4; r++)
        {
            float h = 0.4f + r * 0.45f;
            var c = new Vector3(44f + r * 0.9f, h / 2, L / 2 - 8);
            lit.AddBox(MeshBuilder.Box(c, new Vector3(0.9f, h, 26f)), r % 2 == 0 ? benchWood : Color.Lerp(benchWood, Color.Black, 0.15f));
            for (float z = L / 2 - 20; z < L / 2 + 4; z += 0.7f)
            {
                if (rnd.NextDouble() < 0.6) continue;
                float ph = 0.55f + (float)rnd.NextDouble() * 0.15f;
                var skin = Color.Lerp(new Color(250, 215, 180), new Color(110, 72, 48), (float)rnd.NextDouble() * 0.8f);
                lit.AddBox(MeshBuilder.Box(new Vector3(44f + r * 0.9f, h + ph / 2, z), new Vector3(0.3f, ph, 0.42f)),
                    palette[rnd.Next(palette.Length)], skin);
            }
        }

        // lavičky střídačky
        for (int side = -1; side <= 1; side += 2)
        {
            var c = new Vector3(-38f, 0, L / 2 + side * 8f);
            lit.AddBox(MeshBuilder.Box(c + new Vector3(0, 1.0f, 0), new Vector3(1.6f, 0.1f, 6f)), new Color(200, 205, 215));
            lit.AddBox(MeshBuilder.Box(c + new Vector3(-0.75f, 0.5f, 0), new Vector3(0.1f, 1.0f, 6f)), new Color(200, 205, 215));
            lit.AddBox(MeshBuilder.Box(c + new Vector3(0, 0.25f, 0), new Vector3(0.5f, 0.5f, 5.6f)), new Color(40, 60, 120));
        }

        // domky v dálce
        for (int i = 0; i < 14; i++)
        {
            float x = (float)(rnd.NextDouble() * 2 - 1) * 150;
            float z = rnd.NextDouble() < 0.5 ? -110 - (float)rnd.NextDouble() * 40 : L + 110 + (float)rnd.NextDouble() * 40;
            var size = new Vector3(6 + (float)rnd.NextDouble() * 6, 4 + (float)rnd.NextDouble() * 4, 7 + (float)rnd.NextDouble() * 5);
            var p = new Vector3(x, size.Y / 2, z);
            var houseCol = Color.Lerp(new Color(230, 220, 200), new Color(200, 170, 140), (float)rnd.NextDouble());
            lit.AddBox(MeshBuilder.Box(p, size), houseCol);
            Roof(lit, new Vector3(x, size.Y, z), size.X + 0.8f, size.Z + 0.8f, size.Y * 0.5f, winter ? snow : new Color(150, 60, 45));
        }

        if (winter)
        {
            // hromady sněhu u postranních čar a sněhulák
            for (int side = -1; side <= 1; side += 2)
                for (float z = -4; z < L + 4; z += 6.5f)
                    lit.AddSphere(Matrix.CreateScale(1.6f, 0.7f, 5.5f) * Matrix.CreateTranslation(side * 38.5f, 0, z + 3), snow, 4, 8);

            Snowman(lit, new Vector3(-31f, 0, -5f));
        }

        if (s.Clouds) BuildClouds(glow, rnd, L);
    }

    private static void Tree(MeshBuilder b, Random rnd, Vector3 p, bool winter)
    {
        float scale = 0.8f + (float)rnd.NextDouble() * 0.9f;
        var trunk = new Color(95, 65, 40);
        bool conifer = winter || rnd.NextDouble() < 0.45;
        b.AddCylinder(MeshBuilder.Box(p + new Vector3(0, 1.2f * scale, 0), new Vector3(0.45f * scale, 2.4f * scale, 0.45f * scale)), trunk, 6);
        if (conifer)
        {
            var green = Color.Lerp(new Color(30, 85, 45), new Color(45, 110, 55), (float)rnd.NextDouble());
            for (int i = 0; i < 3; i++)
            {
                float w = (3.6f - i * 0.9f) * scale;
                float y = (2.6f + i * 1.6f) * scale;
                b.AddCone(MeshBuilder.Box(p + new Vector3(0, y + 1.1f * scale, 0), new Vector3(w, 2.4f * scale, w)), green);
                if (winter)
                    b.AddCone(MeshBuilder.Box(p + new Vector3(0, y + 1.75f * scale, 0), new Vector3(w * 0.55f, 1.1f * scale, w * 0.55f)),
                        new Color(240, 244, 250));
            }
        }
        else
        {
            var green = Color.Lerp(new Color(55, 120, 45), new Color(95, 150, 55), (float)rnd.NextDouble());
            b.AddSphere(MeshBuilder.Box(p + new Vector3(0, 3.6f * scale, 0), new Vector3(3.6f, 3.2f, 3.6f) * scale), green, 5, 7);
            b.AddSphere(MeshBuilder.Box(p + new Vector3(0.8f, 4.6f * scale, 0.3f), new Vector3(2.4f, 2.2f, 2.4f) * scale),
                Color.Lerp(green, Color.White, 0.08f), 4, 6);
        }
    }

    private static void FenceLine(MeshBuilder b, Vector2 a, Vector2 c, Color col)
    {
        var d = c - a;
        float len = d.Length();
        var dir = d / len;
        int posts = (int)(len / 3f);
        for (int i = 0; i <= posts; i++)
        {
            var p = a + dir * (len * i / posts);
            b.AddCylinder(MeshBuilder.Box(new Vector3(p.X, 0.6f, p.Y), new Vector3(0.08f, 1.2f, 0.08f)), col, 6, false);
        }
        var along = new Vector3(dir.X, 0, dir.Y);
        var side = Vector3.Cross(Vector3.Up, along);
        foreach (var y in new[] { 0.5f, 1.1f })
        {
            var mid = new Vector3((a.X + c.X) / 2, y, (a.Y + c.Y) / 2);
            b.AddBox(MeshBuilder.Basis(along * len, Vector3.Up * 0.05f, side * 0.05f, mid), col);
        }
    }

    /// <summary>Sedlová střecha (hranol) nad obdélníkem w×d.</summary>
    private static void Roof(MeshBuilder b, Vector3 baseCenter, float w, float d, float h, Color col)
    {
        float hw = w / 2, hd = d / 2;
        var top0 = baseCenter + new Vector3(0, h, -hd);
        var top1 = baseCenter + new Vector3(0, h, hd);
        var l0 = baseCenter + new Vector3(-hw, 0, -hd);
        var l1 = baseCenter + new Vector3(-hw, 0, hd);
        var r0 = baseCenter + new Vector3(hw, 0, -hd);
        var r1 = baseCenter + new Vector3(hw, 0, hd);
        var nl = Vector3.Normalize(new Vector3(-h, hw, 0));
        var nr = Vector3.Normalize(new Vector3(h, hw, 0));
        b.AddQuad(l0, l1, top1, top0, nl, col);
        b.AddQuad(r0, r1, top1, top0, nr, col);
        var gable = Color.Lerp(col, Color.Black, 0.2f);
        b.AddTriangle(l0, r0, top0, -Vector3.UnitZ, gable);
        b.AddTriangle(l1, r1, top1, Vector3.UnitZ, gable);
    }

    private static void Snowman(MeshBuilder b, Vector3 p)
    {
        var snow = new Color(248, 250, 255);
        b.AddSphere(MeshBuilder.Box(p + new Vector3(0, 0.5f, 0), new Vector3(1.1f)), snow, 7, 10);
        b.AddSphere(MeshBuilder.Box(p + new Vector3(0, 1.3f, 0), new Vector3(0.8f)), snow, 7, 10);
        b.AddSphere(MeshBuilder.Box(p + new Vector3(0, 1.9f, 0), new Vector3(0.55f)), snow, 7, 10);
        b.AddCone(Matrix.CreateScale(0.1f, 0.35f, 0.1f) * Matrix.CreateRotationX(MathHelper.PiOver2)
                  * Matrix.CreateTranslation(p + new Vector3(0, 1.9f, 0.4f)), new Color(240, 120, 30), 6);
        var coal = new Color(20, 20, 25);
        b.AddBox(MeshBuilder.Box(p + new Vector3(0.1f, 2.0f, 0.25f), new Vector3(0.07f)), coal);
        b.AddBox(MeshBuilder.Box(p + new Vector3(-0.1f, 2.0f, 0.25f), new Vector3(0.07f)), coal);
        b.AddCylinder(MeshBuilder.Box(p + new Vector3(0, 2.25f, 0), new Vector3(0.45f, 0.05f, 0.45f)), coal, 10);
        b.AddCylinder(MeshBuilder.Box(p + new Vector3(0, 2.42f, 0), new Vector3(0.3f, 0.32f, 0.3f)), coal, 10);
        b.AddBox(MeshBuilder.Box(p + new Vector3(0, 1.6f, 0), new Vector3(0.62f, 0.12f, 0.62f)), new Color(200, 30, 40));
    }

    public static void BuildClouds(MeshBuilder glow, Random rnd, float L)
    {
        for (int i = 0; i < 26; i++)
        {
            float a = (float)rnd.NextDouble() * MathHelper.TwoPi;
            float dist = 260 + (float)rnd.NextDouble() * 160;
            var c = new Vector3(MathF.Cos(a) * dist, 70 + (float)rnd.NextDouble() * 50, L / 2 + MathF.Sin(a) * dist);
            int blobs = 4 + rnd.Next(4);
            for (int k = 0; k < blobs; k++)
            {
                var off = new Vector3((float)(rnd.NextDouble() - 0.5) * 40, (float)(rnd.NextDouble() - 0.5) * 6, (float)(rnd.NextDouble() - 0.5) * 20);
                float sz = 14 + (float)rnd.NextDouble() * 16;
                glow.AddSphere(Matrix.CreateScale(sz * 1.6f, sz * 0.55f, sz) * Matrix.CreateTranslation(c + off),
                    Color.Lerp(Color.White, new Color(215, 225, 240), (float)rnd.NextDouble() * 0.6f), 5, 8);
            }
        }
    }
}
