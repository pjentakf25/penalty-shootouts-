using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PenaltyShootout.Core;
using PenaltyShootout.Data;
using PenaltyShootout.UI;

namespace PenaltyShootout.Scenes;

/// <summary>
/// Nastavení zápasu v krocích: tvůj tým → soupeř → brankář soupeře → prostředí.
/// </summary>
public sealed class SetupScene : Scene
{
    private enum Step { HomeTeam, AwayTeam, Keeper, Arena }

    private Step _step = Step.HomeTeam;
    private readonly SelectionGrid _grid = new();
    private readonly IReadOnlyList<Team> _teams = TeamDatabase.Teams;

    private Team _home, _away;
    private Goalkeeper _awayKeeper;
    private int _previewArena = -1;

    public SetupScene(PenaltyGame game) : base(game)
    {
        _grid.OnMove = game.Audio.Click;
        EnterStep(Step.HomeTeam);
    }

    private List<Team> AwayCandidates => _teams.Where(t => t != _home).ToList();

    private void EnterStep(Step step)
    {
        _step = step;
        _grid.Selected = 0;
        _grid.IsEnabled = _ => true;
        switch (step)
        {
            case Step.HomeTeam:
                _grid.Count = _teams.Count;
                _grid.Columns = 3;
                _grid.RectFor = TeamRect;
                break;
            case Step.AwayTeam:
                _grid.Count = AwayCandidates.Count;
                _grid.Columns = 3;
                _grid.RectFor = TeamRect;
                break;
            case Step.Keeper:
                _grid.Count = _away.Keepers.Count;
                _grid.Columns = 1;
                _grid.RectFor = KeeperRect;
                break;
            case Step.Arena:
                _grid.Count = ArenaStyle.All.Length;
                _grid.Columns = 4;
                _grid.RectFor = ArenaRect;
                _previewArena = -1;
                break;
        }
    }

    private static Rectangle TeamRect(int i) => new(130 + (i % 3) * 345, 150 + (i / 3) * 150, 330, 132);
    private static Rectangle KeeperRect(int i) => new(Ui.Width / 2 - 260, 190 + i * 130, 520, 112);
    private static Rectangle ArenaRect(int i) => new(70 + i * 290, 470, 275, 150);

    public override void Update(float dt)
    {
        if (_step == Step.Arena)
        {
            Game.Renderer.Camera.Position = new Vector3(-9f, 5f, 24f);
            Game.Renderer.Camera.Target = new Vector3(0, 1.5f, 2f);
            if (_previewArena != _grid.Selected)
            {
                _previewArena = _grid.Selected;
                Game.SetArena(ArenaStyle.All[_previewArena], _home.Shirt, _away.Shirt);
            }
        }
        else Game.OrbitCamera();

        if (Input.Back)
        {
            Game.Audio.Click();
            if (_step == Step.HomeTeam) Game.Scenes.Change(new MainMenuScene(Game));
            else EnterStep(_step - 1);
            return;
        }

        if (!_grid.Update()) return;
        Game.Audio.Click();

        switch (_step)
        {
            case Step.HomeTeam:
                _home = _teams[_grid.Selected];
                EnterStep(Step.AwayTeam);
                break;
            case Step.AwayTeam:
                _away = AwayCandidates[_grid.Selected];
                EnterStep(Step.Keeper);
                break;
            case Step.Keeper:
                _awayKeeper = _away.Keepers[_grid.Selected];
                EnterStep(Step.Arena);
                break;
            case Step.Arena:
                var arena = ArenaStyle.All[_grid.Selected];
                var match = new MatchState(_home, _away, _home.BestKeeper, _awayKeeper, arena);
                Game.Scenes.Change(new KickerSelectScene(Game, match));
                break;
        }
    }

    public override void Draw(float dt)
    {
        Game.DrawWorld();
        var sb = Game.SpriteBatch;
        Ui.Begin(sb);
        Ui.Fade(sb, Color.Black * (_step == Step.Arena ? 0.1f : 0.45f));

        string title = _step switch
        {
            Step.HomeTeam => "VYBER SVŮJ TÝM",
            Step.AwayTeam => "VYBER SOUPEŘE",
            Step.Keeper => "KDO BUDE CHYTAT ZA SOUPEŘE?",
            _ => "VYBER PROSTŘEDÍ",
        };
        Ui.Text(sb, title, new Vector2(Ui.Width / 2f, 40), Color.White, 0.8f, Align.Center, title: true);
        Ui.Text(sb, $"Krok {(int)_step + 1} / 4", new Vector2(Ui.Width / 2f, 110), Ui.Accent, 0.8f, Align.Center);

        switch (_step)
        {
            case Step.HomeTeam:
                for (int i = 0; i < _teams.Count; i++) TeamCard(sb, TeamRect(i), _teams[i], i == _grid.Selected);
                break;
            case Step.AwayTeam:
                var away = AwayCandidates;
                for (int i = 0; i < away.Count; i++) TeamCard(sb, TeamRect(i), away[i], i == _grid.Selected);
                break;
            case Step.Keeper:
                Ui.Text(sb, $"{_away.Name} - horší brankář nechytí skoro nic, top brankář skoro všechno.",
                    new Vector2(Ui.Width / 2f, 145), Color.White * 0.85f, 0.8f, Align.Center);
                for (int i = 0; i < _away.Keepers.Count; i++)
                    Widgets.KeeperCard(sb, KeeperRect(i), _away.Keepers[i], _away, i == _grid.Selected);
                break;
            case Step.Arena:
                for (int i = 0; i < ArenaStyle.All.Length; i++) ArenaCard(sb, ArenaRect(i), ArenaStyle.For(ArenaStyle.All[i]), i == _grid.Selected);
                Ui.Text(sb, $"{_home.Name}  vs  {_away.Name}   |   brankář soupeře: {_awayKeeper.Name} ({_awayKeeper.Rating})",
                    new Vector2(Ui.Width / 2f, 420), Color.White, 0.85f, Align.Center);
                break;
        }

        Widgets.Hint(sb, "Šipky / myš - výběr     Enter - potvrdit     Esc - zpět");
        sb.End();
    }

    private static void TeamCard(SpriteBatch sb, Rectangle r, Team t, bool selected)
    {
        Ui.Panel(sb, r, selected ? new Color(30, 55, 95, 235) : Ui.PanelColor, selected ? Ui.Accent : t.Shirt);
        if (selected) Ui.Frame(sb, r, Ui.Accent, 2);
        Widgets.Flag(sb, new Rectangle(r.X + 16, r.Y + 18, 60, 40), t);
        Ui.Text(sb, t.Name, new Vector2(r.X + 90, r.Y + 14), Color.White);
        Ui.Text(sb, t.Code, new Vector2(r.X + 90, r.Y + 40), Ui.Muted, 0.75f);

        // dres
        Ui.Rect(sb, new Rectangle(r.Right - 56, r.Y + 16, 36, 28), t.Shirt);
        Ui.Rect(sb, new Rectangle(r.Right - 52, r.Y + 44, 28, 14), t.Shorts);
        Ui.Frame(sb, new Rectangle(r.Right - 56, r.Y + 16, 36, 28), Color.Black * 0.4f, 1);

        Widgets.StatBar(sb, new Vector2(r.X + 16, r.Y + 76), r.Width - 32, "Útok", t.AttackRating);
        Widgets.StatBar(sb, new Vector2(r.X + 16, r.Y + 100), r.Width - 32, "Brankář", t.BestKeeper.Rating);
    }

    private static void ArenaCard(SpriteBatch sb, Rectangle r, ArenaStyle s, bool selected)
    {
        Ui.Panel(sb, r, selected ? new Color(30, 55, 95, 240) : Ui.PanelColor, selected ? Ui.Accent : Color.White * 0.5f);
        if (selected) Ui.Frame(sb, r, Ui.Accent, 3);
        // malý náhled oblohy a trávy
        var preview = new Rectangle(r.X + 14, r.Y + 16, r.Width - 28, 50);
        Ui.Rect(sb, new Rectangle(preview.X, preview.Y, preview.Width, 30), s.SkyTop);
        Ui.Rect(sb, new Rectangle(preview.X, preview.Y + 18, preview.Width, 12), s.SkyBottom);
        Ui.Rect(sb, new Rectangle(preview.X, preview.Y + 30, preview.Width, 20), s.Snow ? s.Surround : s.Grass1);
        Ui.Text(sb, s.Name, new Vector2(r.X + 14, r.Y + 76), Color.White, 0.9f);
        Ui.Text(sb, s.Description, new Vector2(r.X + 14, r.Y + 108), Ui.Muted, 0.6f);
    }
}
