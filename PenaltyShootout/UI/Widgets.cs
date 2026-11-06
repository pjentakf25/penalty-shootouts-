using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PenaltyShootout.Data;

namespace PenaltyShootout.UI;

/// <summary>Opakovaně použité kousky UI: skóre, karty hráčů, vlajky, tlačítka.</summary>
public static class Widgets
{
    public static void Flag(SpriteBatch sb, Rectangle r, Team t)
    {
        var c = t.Flag;
        int n = c.Length;
        switch (t.FlagStyle)
        {
            case FlagStyle.Vertical:
                for (int i = 0; i < n; i++)
                {
                    int x0 = r.X + r.Width * i / n, x1 = r.X + r.Width * (i + 1) / n;
                    Ui.Rect(sb, new Rectangle(x0, r.Y, x1 - x0, r.Height), c[i]);
                }
                break;

            case FlagStyle.Cross: // Anglie – červený kříž na bílé
                Ui.Rect(sb, r, c[0]);
                int bh = Math.Max(2, r.Height / 5), bw = Math.Max(2, r.Width / 8);
                Ui.Rect(sb, new Rectangle(r.X, r.Center.Y - bh / 2, r.Width, bh), c[1]);
                Ui.Rect(sb, new Rectangle(r.Center.X - bw / 2, r.Y, bw, r.Height), c[1]);
                break;

            case FlagStyle.Czech: // bílá/červená + modrý klín do půlky
                Ui.Rect(sb, new Rectangle(r.X, r.Y, r.Width, r.Height / 2), c[0]);
                Ui.Rect(sb, new Rectangle(r.X, r.Y + r.Height / 2, r.Width, r.Height - r.Height / 2), c[1]);
                int half = r.Width / 2;
                for (int x = 0; x < half; x++)
                {
                    float hh = r.Height / 2f * (1f - x / (float)half);
                    Ui.Rect(sb, new Rectangle(r.X + x, (int)(r.Center.Y - hh), 1, (int)MathF.Ceiling(hh * 2)), c[2]);
                }
                break;

            case FlagStyle.Brazil: // zelená, žlutý kosočtverec, modrý kruh
                Ui.Rect(sb, r, c[0]);
                int m = Math.Max(2, r.Width / 12);
                float halfW = r.Width / 2f - m;
                for (int x = r.X + m; x < r.Right - m; x++)
                {
                    float hh = (r.Height / 2f - m * 0.7f) * (1f - MathF.Abs(x + 0.5f - r.Center.X) / halfW);
                    if (hh <= 0) continue;
                    Ui.Rect(sb, new Rectangle(x, (int)(r.Center.Y - hh), 1, (int)MathF.Ceiling(hh * 2)), c[1]);
                }
                Ui.Dot(sb, r.Center.ToVector2(), r.Height * 0.24f, c[2]);
                break;

            default:
                for (int i = 0; i < n; i++)
                {
                    int y0 = r.Y + r.Height * i / n, y1 = r.Y + r.Height * (i + 1) / n;
                    Ui.Rect(sb, new Rectangle(r.X, y0, r.Width, y1 - y0), c[i]);
                }
                break;
        }
        Ui.Frame(sb, r, Color.Black * 0.5f, 1);
    }

    /// <summary>Tabule skóre nahoře uprostřed s tečkami za každou penaltu.</summary>
    public static void Scoreboard(SpriteBatch sb, MatchState m)
    {
        var r = new Rectangle(Ui.Width / 2 - 300, 10, 600, 92);
        Ui.Panel(sb, r);

        Flag(sb, new Rectangle(r.X + 16, r.Y + 16, 42, 28), m.Home);
        Ui.Text(sb, m.Home.Code, new Vector2(r.X + 66, r.Y + 14), Color.White);
        Flag(sb, new Rectangle(r.Right - 58, r.Y + 16, 42, 28), m.Away);
        Ui.Text(sb, m.Away.Code, new Vector2(r.Right - 66, r.Y + 14), Color.White, 1f, Align.Right);

        Ui.Text(sb, $"{m.HomeGoals} : {m.AwayGoals}", new Vector2(r.Center.X, r.Y + 2), Color.White, 0.68f, Align.Center, title: true);

        int slots = Math.Max(MatchState.RegularRounds, Math.Max(m.HomeKicks.Count, m.AwayKicks.Count));
        DrawDots(sb, m.HomeKicks, slots, new Vector2(r.X + 30, r.Y + 68), 1);
        DrawDots(sb, m.AwayKicks, slots, new Vector2(r.Right - 30, r.Y + 68), -1);

        string info = m.SuddenDeath ? "NÁHLÁ SMRT" : $"SÉRIE {Math.Min(m.Round, MatchState.RegularRounds)}/{MatchState.RegularRounds}";
        Ui.Text(sb, info, new Vector2(r.Center.X, r.Y + 62), Ui.Accent, 0.68f, Align.Center);
    }

    private static void DrawDots(SpriteBatch sb, List<KickRecord> kicks, int slots, Vector2 start, int dir)
    {
        int shown = Math.Min(slots, 7);
        int first = Math.Max(0, slots - shown);
        for (int i = 0; i < shown; i++)
        {
            int idx = first + i;
            var p = start + new Vector2(dir * i * 26, 0);
            if (idx < kicks.Count)
                Ui.Dot(sb, p, 9, kicks[idx].Scored ? Ui.GoalGreen : Ui.MissRed);
            else
                Ui.RingAt(sb, p, 9, Color.White * 0.6f);
        }
    }

    /// <summary>Karta exekutora se statistikami.</summary>
    public static void KickerCard(SpriteBatch sb, Rectangle r, Kicker k, Team t, bool selected, bool used, KickRecord record)
    {
        var fill = selected ? new Color(30, 55, 95, 235) : Ui.PanelColor;
        Ui.Panel(sb, r, fill, selected ? Ui.Accent : t.Shirt);
        if (selected) Ui.Frame(sb, r, Ui.Accent, 2);

        float alpha = used ? 0.45f : 1f;
        Ui.Text(sb, k.Name, new Vector2(r.X + 14, r.Y + 10), Color.White * alpha);
        Ui.Text(sb, k.Position.Czech(), new Vector2(r.X + 14, r.Y + 40), Ui.Muted * alpha, 0.72f);
        Ui.Text(sb, k.PenaltySkill.ToString(), new Vector2(r.Right - 14, r.Y + 8), Ui.Accent * alpha, 0.65f, Align.Right, title: true);

        StatBar(sb, new Vector2(r.X + 14, r.Y + 70), r.Width - 28, "Razance", k.Power, alpha);
        StatBar(sb, new Vector2(r.X + 14, r.Y + 93), r.Width - 28, "Umístění", k.Accuracy, alpha);

        if (used && record != null)
        {
            var c = record.Scored ? Ui.GoalGreen : Ui.MissRed;
            Ui.Text(sb, record.Scored ? "GÓL" : "NEDAL", new Vector2(r.Right - 14, r.Y + 38), c, 0.8f, Align.Right);
        }
    }

    public static void StatBar(SpriteBatch sb, Vector2 pos, int width, string label, int value, float alpha = 1f)
    {
        Ui.Text(sb, label, pos + new Vector2(0, -3), Ui.Muted * alpha, 0.65f, shadow: false);
        var bar = new Rectangle((int)pos.X + 92, (int)pos.Y + 2, width - 130, 10);
        Ui.Bar(sb, bar, value / 100f, StatColor(value) * alpha);
        Ui.Text(sb, value.ToString(), new Vector2(pos.X + width, pos.Y - 3), Color.White * alpha, 0.65f, Align.Right, shadow: false);
    }

    public static Color StatColor(int v) => v switch
    {
        >= 85 => new Color(70, 210, 100),
        >= 75 => new Color(170, 215, 60),
        >= 65 => new Color(250, 200, 40),
        _ => new Color(235, 110, 50),
    };

    public static void KeeperCard(SpriteBatch sb, Rectangle r, Goalkeeper g, Team t, bool selected)
    {
        var fill = selected ? new Color(30, 55, 95, 235) : Ui.PanelColor;
        Ui.Panel(sb, r, fill, selected ? Ui.Accent : t.KeeperShirt);
        if (selected) Ui.Frame(sb, r, Ui.Accent, 2);
        Ui.Text(sb, g.Name, new Vector2(r.X + 14, r.Y + 10), Color.White);
        Ui.Text(sb, $"Brankář  -  {g.Tier}", new Vector2(r.X + 14, r.Y + 36), Ui.Muted, 0.75f);
        Ui.Text(sb, g.Rating.ToString(), new Vector2(r.Right - 14, r.Y + 8), Ui.Accent, 0.65f, Align.Right, title: true);
        StatBar(sb, new Vector2(r.X + 14, r.Y + 64), r.Width - 28, "Chytání", g.Rating);
    }

    public static void Button(SpriteBatch sb, Rectangle r, string text, bool selected)
    {
        Ui.Panel(sb, r, selected ? new Color(255, 200, 40, 240) : Ui.PanelColor, selected ? Color.White : Ui.Accent);
        Ui.Text(sb, text, new Vector2(r.Center.X, r.Center.Y - 13), selected ? new Color(20, 20, 30) : Color.White, 1f, Align.Center, shadow: !selected);
    }

    public static void Hint(SpriteBatch sb, string text)
    {
        var size = Ui.Measure(text, 0.8f);
        var r = new Rectangle((int)(Ui.Width / 2 - size.X / 2 - 16), Ui.Height - 46, (int)size.X + 32, 32);
        Ui.Rect(sb, r, Color.Black * 0.5f);
        Ui.Text(sb, text, new Vector2(Ui.Width / 2f, Ui.Height - 42), Color.White, 0.8f, Align.Center);
    }
}
