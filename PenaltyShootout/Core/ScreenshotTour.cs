using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PenaltyShootout.Data;
using PenaltyShootout.Scenes;

namespace PenaltyShootout.Core;

/// <summary>
/// Ladicí režim: spuštění s parametrem <c>--screenshots &lt;složka&gt;</c> projde
/// hlavní obrazovky, uloží PNG snímky a hru ukončí.
/// </summary>
public sealed class ScreenshotTour
{
    private readonly string _dir;
    private readonly List<(string Name, Arena Arena, Func<PenaltyGame, MatchState, Scene> Create, float Wait)> _steps;
    private int _index = -1;
    private float _timer;
    private MatchState _match;

    public ScreenshotTour(string dir)
    {
        _dir = dir;
        Directory.CreateDirectory(dir);
        _steps = new()
        {
            ("01_menu", Arena.StadiumDay, (g, m) => new MainMenuScene(g), 1.5f),
            ("02_setup", Arena.StadiumDay, (g, m) => new SetupScene(g), 1.0f),
            ("03_kicker_select", Arena.StadiumDay, (g, m) => new KickerSelectScene(g, m), 1.0f),
            ("04_aim", Arena.StadiumDay, (g, m) => new PenaltyScene(g, m, true, m.Home.Kickers[0]), 2.2f),
            ("05_ai_runup", Arena.StadiumNight, (g, m) => new PenaltyScene(g, m, false, m.Away.Kickers[0]), 3.85f),
            ("06_ai_result", Arena.StadiumNight, (g, m) => new PenaltyScene(g, m, false, m.Away.Kickers[1]), 4.9f),
            ("07_park", Arena.Park, (g, m) => new PenaltyScene(g, m, false, m.Away.Kickers[2]), 3.3f),
            ("08_winter", Arena.Winter, (g, m) => new PenaltyScene(g, m, false, m.Away.Kickers[3]), 4.7f),
            ("09_result", Arena.StadiumDay, (g, m) => { Fill(m); return new ResultScene(g, m); }, 1.2f),
        };
    }

    private static void Fill(MatchState m)
    {
        var r = new Random(3);
        for (int i = 0; i < 5; i++)
        {
            m.Record(true, m.Home.Kickers[i], r.NextDouble() < 0.7 ? Gameplay.ShotOutcome.Goal : Gameplay.ShotOutcome.Saved);
            if (m.IsFinished(out _)) break;
            m.Record(false, m.Away.Kickers[i], r.NextDouble() < 0.5 ? Gameplay.ShotOutcome.Goal : Gameplay.ShotOutcome.Wide);
            if (m.IsFinished(out _)) break;
        }
    }

    public void Update(PenaltyGame game, float dt)
    {
        _timer -= dt;
        if (_index >= 0 && _timer > 0) return;
        if (_index + 1 >= _steps.Count) return;
        // přechod na další krok proběhne po uložení snímku v AfterDraw
        if (_index < 0) Next(game);
    }

    private void Next(PenaltyGame game)
    {
        _index++;
        if (_index >= _steps.Count) { game.Exit(); return; }
        var step = _steps[_index];
        var teams = TeamDatabase.Teams;
        _match = new MatchState(teams[0], teams[2], teams[0].BestKeeper, teams[2].Keepers[0], step.Arena);
        game.SetArena(step.Arena, _match.Home.Shirt, _match.Away.Shirt);
        game.Scenes.Change(step.Create(game, _match));
        _timer = step.Wait;
    }

    public void AfterDraw(PenaltyGame game)
    {
        if (_index < 0 || _timer > 0) return;
        var gd = game.GraphicsDevice;
        int w = gd.PresentationParameters.BackBufferWidth, h = gd.PresentationParameters.BackBufferHeight;
        var data = new Color[w * h];
        gd.GetBackBufferData(data);
        using (var tex = new Texture2D(gd, w, h))
        {
            tex.SetData(data);
            using var fs = File.Create(Path.Combine(_dir, _steps[_index].Name + ".png"));
            tex.SaveAsPng(fs, w, h);
        }
        Next(game);
    }
}
