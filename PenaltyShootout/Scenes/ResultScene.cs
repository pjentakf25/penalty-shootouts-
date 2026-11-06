using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PenaltyShootout.Core;
using PenaltyShootout.Data;
using PenaltyShootout.UI;

namespace PenaltyShootout.Scenes;

/// <summary>Konec rozstřelu – výsledek a přehled všech penalt.</summary>
public sealed class ResultScene : Scene
{
    private static readonly string[] Buttons = { "Odveta", "Nový zápas", "Hlavní menu" };

    private readonly MatchState _match;
    private readonly bool _homeWins;
    private readonly SelectionGrid _menu;
    private float _time;

    public ResultScene(PenaltyGame game, MatchState match) : base(game)
    {
        _match = match;
        match.IsFinished(out _homeWins);
        _menu = new SelectionGrid { Count = Buttons.Length, Columns = 3, RectFor = ButtonRect, OnMove = game.Audio.Click };
    }

    public override void Enter()
    {
        float crowd = Game.World.Style.HasStands ? 1f : 0.4f;
        if (_homeWins) Game.Audio.Cheer(crowd); else Game.Audio.Groan(crowd);
    }

    private static Rectangle ButtonRect(int i) => new(Ui.Width / 2 - 390 + i * 270, 610, 240, 58);

    public override void Update(float dt)
    {
        _time += dt;
        Game.OrbitCamera(0.1f, 22f, 6f);
        if (_time < 0.6f) return;

        if (Input.Back) { Game.Scenes.Change(new MainMenuScene(Game)); return; }
        if (!_menu.Update()) return;

        Game.Audio.Click();
        switch (_menu.Selected)
        {
            case 0:
                var rematch = new MatchState(_match.Home, _match.Away, _match.HomeKeeper, _match.AwayKeeper, _match.Arena);
                Game.Scenes.Change(new KickerSelectScene(Game, rematch));
                break;
            case 1: Game.Scenes.Change(new SetupScene(Game)); break;
            default: Game.Scenes.Change(new MainMenuScene(Game)); break;
        }
    }

    public override void Draw(float dt)
    {
        Game.DrawWorld();
        var sb = Game.SpriteBatch;
        Ui.Begin(sb);
        Ui.Fade(sb, Color.Black * 0.45f);

        float pop = MathHelper.Clamp(_time / 0.35f, 0, 1);
        string title = _homeWins ? "VÍTĚZSTVÍ!" : "PROHRA";
        var color = _homeWins ? Ui.GoalGreen : Ui.MissRed;
        Ui.TextCentered(sb, title, new Vector2(Ui.Width / 2f, 80), color, 1.2f * (0.6f + 0.4f * pop));

        var winner = _homeWins ? _match.Home : _match.Away;
        Ui.Text(sb, $"{winner.Name} vyhrává penaltový rozstřel", new Vector2(Ui.Width / 2f, 140), Color.White, 0.9f, Align.Center);

        var score = new Rectangle(Ui.Width / 2 - 260, 180, 520, 90);
        Ui.Panel(sb, score);
        Widgets.Flag(sb, new Rectangle(score.X + 20, score.Y + 25, 60, 40), _match.Home);
        Widgets.Flag(sb, new Rectangle(score.Right - 80, score.Y + 25, 60, 40), _match.Away);
        Ui.Text(sb, _match.Home.Code, new Vector2(score.X + 92, score.Y + 32), Color.White);
        Ui.Text(sb, _match.Away.Code, new Vector2(score.Right - 92, score.Y + 32), Color.White, 1f, Align.Right);
        Ui.Text(sb, $"{_match.HomeGoals} : {_match.AwayGoals}", new Vector2(score.Center.X, score.Y + 6), Ui.Accent, 1f, Align.Center, title: true);

        KickList(sb, new Rectangle(Ui.Width / 2 - 560, 290, 540, 300), _match.Home, _match.HomeKicks);
        KickList(sb, new Rectangle(Ui.Width / 2 + 20, 290, 540, 300), _match.Away, _match.AwayKicks);

        for (int i = 0; i < Buttons.Length; i++) Widgets.Button(sb, ButtonRect(i), Buttons[i], i == _menu.Selected);
        sb.End();
    }

    private static void KickList(SpriteBatch sb, Rectangle r, Team team, List<KickRecord> kicks)
    {
        Ui.Panel(sb, r, null, team.Shirt);
        Ui.Text(sb, team.Name, new Vector2(r.X + 16, r.Y + 10), Color.White);
        int rows = Math.Min(kicks.Count, 9);
        int first = kicks.Count - rows;
        for (int i = 0; i < rows; i++)
        {
            var k = kicks[first + i];
            float y = r.Y + 46 + i * 27;
            Ui.Dot(sb, new Vector2(r.X + 26, y + 11), 8, k.Scored ? Ui.GoalGreen : Ui.MissRed);
            Ui.Text(sb, $"{first + i + 1}.  {k.Kicker.Name}", new Vector2(r.X + 44, y), Color.White, 0.75f);
            string res = Gameplay.ShotResolver.ShortLabel(k.Outcome);
            Ui.Text(sb, res, new Vector2(r.Right - 16, y), k.Scored ? Ui.GoalGreen : Ui.MissRed, 0.75f, Align.Right);
        }
    }
}
