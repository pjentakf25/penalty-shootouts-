using Microsoft.Xna.Framework;
using PenaltyShootout.Core;
using PenaltyShootout.Data;
using PenaltyShootout.Gameplay;
using PenaltyShootout.UI;
using PenaltyShootout.World;

namespace PenaltyShootout.Scenes;

/// <summary>Před každou sérií: kdo z tvého týmu půjde kopat?</summary>
public sealed class KickerSelectScene : Scene
{
    private readonly MatchState _match;
    private readonly SelectionGrid _grid;
    private readonly KeeperController _keeper;
    private readonly Ball _ball = new();

    public KickerSelectScene(PenaltyGame game, MatchState match) : base(game)
    {
        _match = match;
        _grid = new SelectionGrid
        {
            Count = match.Home.Kickers.Count,
            Columns = 2,
            RectFor = CardRect,
            IsEnabled = i => !_match.IsUsed(_match.Home.Kickers[i]),
            OnMove = game.Audio.Click,
        };
        // vyber prvního volného
        for (int i = 0; i < _grid.Count; i++)
            if (_grid.IsEnabled(i)) { _grid.Selected = i; break; }

        _keeper = new KeeperController(Appearance.ForKeeper(match.AwayKeeper, match.Away));
        _ball.Reset();
    }

    public override void Enter()
    {
        if (Game.World.Style.Arena != _match.Arena)
            Game.SetArena(_match.Arena, _match.Home.Shirt, _match.Away.Shirt);
    }

    private static Rectangle CardRect(int i) => new(Ui.Width / 2 - 590 + (i % 2) * 600, 150 + (i / 2) * 128, 580, 116);

    public override void Update(float dt)
    {
        var cam = Game.Renderer.Camera;
        cam.Position = new Vector3(2.5f + System.MathF.Sin(Game.Time * 0.3f) * 0.8f, 2.2f, 17.5f);
        cam.Target = new Vector3(0, 1.2f, 0);
        _keeper.Update(dt);

        if (Input.Back)
        {
            Game.Scenes.Change(new MainMenuScene(Game));
            return;
        }

        if (_grid.Update() && _grid.IsEnabled(_grid.Selected))
        {
            Game.Audio.Click();
            var kicker = _match.Home.Kickers[_grid.Selected];
            Game.Scenes.Change(new PenaltyScene(Game, _match, true, kicker));
        }
    }

    public override void Draw(float dt)
    {
        Game.DrawWorld(r =>
        {
            _keeper.Model.Draw(r);
            _ball.Draw(r);
        }, r =>
        {
            _keeper.Model.DrawShadow(r);
            _ball.DrawShadow(r);
        });

        var sb = Game.SpriteBatch;
        Ui.Begin(sb);
        Ui.Fade(sb, Color.Black * 0.45f);
        Widgets.Scoreboard(sb, _match);

        Ui.Text(sb, $"Série {_match.Round}: vyber exekutora  -  proti tobě chytá {_match.AwayKeeper.Name} ({_match.AwayKeeper.Rating})",
            new Vector2(Ui.Width / 2f, 112), Color.White, 0.85f, Align.Center);

        var team = _match.Home;
        for (int i = 0; i < team.Kickers.Count; i++)
        {
            var k = team.Kickers[i];
            bool used = _match.IsUsed(k);
            Widgets.KickerCard(sb, CardRect(i), k, team, i == _grid.Selected && !used, used, _match.HomeRecordOf(k));
        }

        Widgets.Hint(sb, "Šipky / myš - výběr hráče     Enter - na značku     Esc - menu");
        sb.End();
    }
}
