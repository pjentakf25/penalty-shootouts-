using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PenaltyShootout.Core;
using PenaltyShootout.Data;
using PenaltyShootout.Gameplay;
using PenaltyShootout.UI;
using PenaltyShootout.World;

namespace PenaltyShootout.Scenes;

/// <summary>
/// Jedna penalta ve 3D.
/// Když kope hráč: 1) směr do strany, 2) výška, 3) síla – vždy mezerník / klik ve správnou chvíli.
/// Když kope soupeř: hráč chytá – A/D/W určí, kam brankář skočí.
/// </summary>
public sealed class PenaltyScene : Scene
{
    private enum Phase { Intro, AimX, AimY, Power, Wait, RunUp, Flight, Result }

    private static readonly Vector3 CamPos = new(0.6f, 2.45f, 19.6f);
    private static readonly Vector3 CamTarget = new(0, 1.0f, 0);

    private readonly MatchState _match;
    private readonly bool _playerShoots;
    private readonly Kicker _kicker;
    private readonly Team _shootTeam, _keepTeam;
    private readonly Goalkeeper _keeper;

    private readonly KickerController _kickerCtl;
    private readonly KeeperController _keeperCtl;
    private readonly Ball _ball = new();

    private Phase _phase = Phase.Intro;
    private float _phaseTime;
    private bool _paused;

    // ukazatele
    private float _meter;
    private float _aimX, _aimY = 1.2f, _power;

    // střela
    private ShotPlan _plan;
    private ShotOutcome _outcome;
    private int? _playerDive; // -1 vlevo, 0 střed, 1 vpravo
    private bool _arrived;
    private float _flightTime;

    // nápis výsledku
    private string _banner;
    private Color _bannerColor;
    private float _bannerTime;
    private float _shake;

    public PenaltyScene(PenaltyGame game, MatchState match, bool playerShoots, Kicker kicker) : base(game)
    {
        _match = match;
        _playerShoots = playerShoots;
        _kicker = kicker;
        _shootTeam = playerShoots ? match.Home : match.Away;
        _keepTeam = playerShoots ? match.Away : match.Home;
        _keeper = playerShoots ? match.AwayKeeper : match.HomeKeeper;

        _kickerCtl = new KickerController(Appearance.ForKicker(kicker, _shootTeam));
        _keeperCtl = new KeeperController(Appearance.ForKeeper(_keeper, _keepTeam));
        _ball.Reset();
    }

    private void SetPhase(Phase p)
    {
        _phase = p;
        _phaseTime = 0;
        _meter = 0;
    }

    // ================================================================ UPDATE

    public override void Update(float dt)
    {
        if (_paused)
        {
            if (Input.Pressed(Keys.Enter) || Input.Pressed(Keys.Space) || Input.LeftClick) _paused = false;
            else if (Input.Pressed(Keys.Escape)) Game.Scenes.Change(new MainMenuScene(Game));
            return;
        }
        if (Input.Pressed(Keys.Escape)) { _paused = true; return; }

        _phaseTime += dt;
        _bannerTime += dt;
        _shake = MathF.Max(0, _shake - dt * 2.5f);

        switch (_phase)
        {
            case Phase.Intro:
                if (_phaseTime > 1.4f || (_phaseTime > 0.4f && Input.Action))
                    SetPhase(_playerShoots ? Phase.AimX : Phase.Wait);
                break;

            case Phase.AimX:
                _meter += dt * ShotResolver.AimSpeed(_kicker);
                _aimX = MathF.Sin(_meter) * ShotResolver.AimRangeX;
                if (Input.Action) { Game.Audio.Click(); float x = _aimX; SetPhase(Phase.AimY); _aimX = x; }
                break;

            case Phase.AimY:
                _meter += dt * ShotResolver.AimSpeed(_kicker) * 0.9f;
                _aimY = ShotResolver.AimMinY + (0.5f - 0.5f * MathF.Cos(_meter)) * (ShotResolver.AimMaxY - ShotResolver.AimMinY);
                if (Input.Action) { Game.Audio.Click(); float y = _aimY; SetPhase(Phase.Power); _aimY = y; }
                break;

            case Phase.Power:
                _meter += dt * ShotResolver.PowerSpeed(_kicker);
                float tri = _meter % 2f;
                _power = tri < 1f ? tri : 2f - tri;
                if (Input.Action)
                {
                    _plan = ShotResolver.Build(_kicker, new Vector2(_aimX, _aimY), _power);
                    StartRun();
                }
                break;

            case Phase.Wait:
                ReadDiveInput();
                if (_phaseTime > 1.0f)
                {
                    _plan = ShotResolver.BuildAi(_kicker);
                    StartRun();
                }
                break;

            case Phase.RunUp:
                ReadDiveInput();
                break;

            case Phase.Flight:
                _flightTime += dt;
                if (_ball.ScriptDone && !_arrived) OnBallArrived();
                break;

            case Phase.Result:
                _flightTime += dt;
                if (_phaseTime > 3.0f || (_phaseTime > 1.0f && Input.Action)) Finish();
                break;
        }

        _kickerCtl.Update(dt);
        if (_kickerCtl.JustKicked) Kick();
        _keeperCtl.Update(dt);
        _ball.Update(dt);

        UpdateCamera();
    }

    private void ReadDiveInput()
    {
        if (_playerShoots || _playerDive.HasValue) return;
        if (Input.Left) _playerDive = -1;
        else if (Input.Right) _playerDive = 1;
        else if (Input.Up || Input.Pressed(Keys.Space)) _playerDive = 0;
        if (_playerDive.HasValue) Game.Audio.Click();
    }

    private void StartRun()
    {
        Game.Audio.Whistle();
        _kickerCtl.BeginRun();
        SetPhase(Phase.RunUp);
    }

    /// <summary>Okamžik kontaktu nohy s míčem – rozhodne se výsledek a brankář skočí.</summary>
    private void Kick()
    {
        Game.Audio.Kick();
        _outcome = Resolve();

        var t = _plan.Target;
        float duration = ShotResolver.SpotDistance / _plan.Speed;

        // kam letí míč
        Vector3 end = _outcome switch
        {
            ShotOutcome.Saved => new Vector3(t.X, t.Y, KeeperController.LineZ + 0.1f),
            ShotOutcome.Post or ShotOutcome.PostIn => PostContact(t),
            _ => new Vector3(t.X, t.Y, 0f),
        };
        float flight = duration * (Ball.Spot.Z - end.Z) / Ball.Spot.Z;
        float arc = _plan.Panenka ? 1.3f : 0.12f + (1 - _plan.SpeedNorm) * 0.45f;
        _ball.Launch(end, flight, arc, Rng.Range(-0.25f, 0.25f));

        DiveKeeper(t, flight);
        _flightTime = 0;
        _arrived = false;
        SetPhase(Phase.Flight);
    }

    private ShotOutcome Resolve()
    {
        var t = _plan.Target;
        var frame = ShotResolver.Frame(t);
        if (frame.HasValue) return frame.Value;

        float chance;
        if (_plan.Panenka)
            chance = ShotResolver.PanenkaSaveChance(_keeper.Rating);
        else
            chance = ShotResolver.SaveChance(_keeper.Rating, t, _plan.SpeedNorm);

        if (!_playerShoots)
        {
            // chytá hráč – rozhoduje, jestli uhodl stranu
            if (_playerDive.HasValue)
            {
                int zone = ShotResolver.Zone(t.X);
                if (zone == _playerDive.Value) chance = MathF.Min(0.95f, chance * 1.25f + 0.15f);
                else if (_playerDive.Value == 0 && MathF.Abs(t.X) < 1.6f) chance *= 0.5f;
                else chance = 0.02f;
            }
            else chance *= 0.85f;
        }

        return Rng.Chance(chance) ? ShotOutcome.Saved : ShotOutcome.Goal;
    }

    private static Vector3 PostContact(Vector2 t)
    {
        float w = ShotResolver.GoalHalfWidth, h = ShotResolver.GoalHeight;
        if (t.Y > h - ShotResolver.BallRadius)
            return new Vector3(MathHelper.Clamp(t.X, -w, w), h + 0.02f, 0.05f); // břevno
        return new Vector3(MathF.Sign(t.X) * (w - 0.02f), t.Y, 0.05f);      // tyč
    }

    /// <summary>Kam skočí brankář (rukavice v rovině brány).</summary>
    private void DiveKeeper(Vector2 t, float flight)
    {
        float duration = MathF.Max(0.3f, flight - 0.08f);
        Vector2 hands;

        if (_outcome == ShotOutcome.Saved)
            hands = t;
        else if (!_playerShoots && _playerDive.HasValue)
            hands = _playerDive.Value switch
            {
                -1 => new Vector2(-2.7f, 0.9f),
                1 => new Vector2(2.7f, 0.9f),
                _ => new Vector2(0, 1.9f),
            };
        else if (_plan.Panenka)
            hands = new Vector2(Rng.Sign * 2.7f, 0.8f); // brankář skočí, míč jde doprostřed
        else if (MathF.Abs(t.X) < 1.0f || Rng.Chance(0.55f))
        {
            // špatná strana
            float side = MathF.Abs(t.X) < 1.0f ? Rng.Sign : -MathF.Sign(t.X);
            hands = new Vector2(side * Rng.Range(1.8f, 3f), Rng.Range(0.4f, 1.6f));
        }
        else
        {
            // správná strana, ale nedosáhne
            hands = new Vector2(t.X * 0.62f, MathF.Min(t.Y, 2.2f) * 0.75f);
            duration *= 1.25f;
        }

        _keeperCtl.StartDive(hands, 0.07f, duration);
    }

    private void OnBallArrived()
    {
        _arrived = true;
        var t = _plan.Target;
        switch (_outcome)
        {
            case ShotOutcome.Goal:
                _ball.InNet = true;
                break;
            case ShotOutcome.PostIn:
                Game.Audio.Post();
                _ball.Release(new Vector3(-MathF.Sign(t.X) * 2f, -1f, -6f));
                _ball.InNet = true;
                break;
            case ShotOutcome.Saved:
                if (MathF.Abs(t.X) < 0.9f && t.Y < 1.8f)
                    _ball.Release(new Vector3(0, 0.6f, 1.2f)); // chycený do náruče
                else
                    _ball.Release(new Vector3(MathF.Sign(t.X) * Rng.Range(1f, 4f), Rng.Range(1.5f, 4f), Rng.Range(4f, 8f)));
                break;
            case ShotOutcome.Post:
                Game.Audio.Post();
                _ball.Release(new Vector3(-MathF.Sign(t.X) * Rng.Range(1f, 3f), Rng.Range(0f, 2f), Rng.Range(5f, 9f)));
                break;
        }

        bool scored = _outcome.IsGoal();
        bool good = _playerShoots ? scored : !scored;
        float crowd = Game.World.Style.HasStands ? 1f : 0.35f;
        if (good) Game.Audio.Cheer(crowd); else Game.Audio.Groan(crowd);

        _banner = ShotResolver.Banner(_outcome, _plan.Panenka && scored);
        _bannerColor = scored ? Ui.GoalGreen : _outcome == ShotOutcome.Saved ? new Color(90, 170, 255) : Ui.MissRed;
        _bannerTime = 0;
        _shake = scored ? 0.6f : 0.3f;

        _kickerCtl.Mood = scored ? Mood.Happy : Mood.Sad;
        if (_outcome == ShotOutcome.Saved) _keeperCtl.Celebrate();

        SetPhase(Phase.Result);
    }

    private void Finish()
    {
        _match.Record(home: _playerShoots, _kicker, _outcome);

        if (_match.IsFinished(out _))
        {
            Game.Audio.FinalWhistle();
            Game.Scenes.Change(new ResultScene(Game, _match));
        }
        else if (_playerShoots)
            Game.Scenes.Change(new PenaltyScene(Game, _match, false, _match.NextAwayKicker()));
        else
            Game.Scenes.Change(new KickerSelectScene(Game, _match));
    }

    private void UpdateCamera()
    {
        var cam = Game.Renderer.Camera;
        if (_phase == Phase.Intro)
        {
            float u = MathHelper.SmoothStep(0, 1, MathHelper.Clamp(_phaseTime / 1.3f, 0, 1));
            cam.Position = Vector3.Lerp(new Vector3(7f, 4.5f, 23f), CamPos, u);
            cam.Target = Vector3.Lerp(new Vector3(0, 0.8f, 9f), CamTarget, u);
        }
        else if (_phase is Phase.Flight or Phase.Result)
        {
            float u = MathHelper.SmoothStep(0, 1, MathHelper.Clamp(_flightTime / 0.9f, 0, 1));
            var bx = MathHelper.Clamp(_ball.Position.X, -5, 5);
            cam.Position = CamPos + new Vector3(bx * 0.15f, -0.1f * u, -1.8f * u);
            cam.Target = Vector3.Lerp(CamTarget, new Vector3(bx * 0.6f, 1.1f, 0), 0.35f * u);
        }
        else
        {
            cam.Position = CamPos;
            cam.Target = CamTarget;
        }

        if (_shake > 0)
        {
            float s = _shake * _shake * 0.08f;
            cam.Position += new Vector3(Rng.Range(-s, s), Rng.Range(-s, s), 0);
        }
    }

    // ================================================================== DRAW

    public override void Enter() => Game.Renderer.Camera.FieldOfView = MathHelper.ToRadians(38);
    public override void Leave() => Game.Renderer.Camera.FieldOfView = MathHelper.ToRadians(48);

    public override void Draw(float dt)
    {
        Game.DrawWorld(r =>
        {
            _kickerCtl.Model.Draw(r);
            _keeperCtl.Model.Draw(r);
            _ball.Draw(r);
        }, r =>
        {
            _kickerCtl.Model.DrawShadow(r);
            _keeperCtl.Model.DrawShadow(r);
            _ball.DrawShadow(r);
        });

        var sb = Game.SpriteBatch;
        Ui.Begin(sb);

        Widgets.Scoreboard(sb, _match);
        DrawPlayerCards(sb);

        switch (_phase)
        {
            case Phase.Intro:
                DrawIntro(sb);
                break;
            case Phase.AimX:
            case Phase.AimY:
                DrawAim(sb);
                Widgets.Hint(sb, _phase == Phase.AimX
                    ? "MEZERNÍK / KLIK - zastav SMĚR (do strany)"
                    : "MEZERNÍK / KLIK - zastav VÝŠKU");
                break;
            case Phase.Power:
                DrawAim(sb);
                DrawPowerBar(sb);
                Widgets.Hint(sb, "MEZERNÍK / KLIK - zastav SÍLU (zelená = ideál, červená = přes břevno)");
                break;
            case Phase.Wait:
            case Phase.RunUp:
                if (!_playerShoots) DrawDiveHint(sb);
                break;
            case Phase.Result:
                DrawBanner(sb);
                if (_phaseTime > 1.0f) Widgets.Hint(sb, "MEZERNÍK - pokračovat");
                break;
        }

        if (_paused)
        {
            Ui.Fade(sb, Color.Black * 0.6f);
            Ui.Text(sb, "PAUZA", new Vector2(Ui.Width / 2f, 270), Color.White, 1f, Align.Center, title: true);
            Ui.Text(sb, "Enter - pokračovat     Esc - hlavní menu", new Vector2(Ui.Width / 2f, 380), Color.White, 0.9f, Align.Center);
        }

        sb.End();
    }

    private Vector2 Proj(Vector3 world) => Ui.ScreenToVirtual(Game.Renderer.Project(world));

    private void DrawPlayerCards(SpriteBatch sb)
    {
        // exekutor vlevo dole
        var r = new Rectangle(20, Ui.Height - 146, 360, 126);
        Ui.Panel(sb, r, null, _shootTeam.Shirt);
        Widgets.Flag(sb, new Rectangle(r.X + 14, r.Y + 14, 36, 24), _shootTeam);
        Ui.Text(sb, _kicker.Name, new Vector2(r.X + 60, r.Y + 12), Color.White);
        Ui.Text(sb, $"{_kicker.Position.Czech()}  -  {_shootTeam.Name}", new Vector2(r.X + 14, r.Y + 44), Ui.Muted, 0.72f);
        Widgets.StatBar(sb, new Vector2(r.X + 14, r.Y + 72), r.Width - 28, "Razance", _kicker.Power);
        Widgets.StatBar(sb, new Vector2(r.X + 14, r.Y + 96), r.Width - 28, "Umístění", _kicker.Accuracy);

        // brankář vpravo nahoře
        var k = new Rectangle(Ui.Width - 330, 20, 310, 96);
        Ui.Panel(sb, k, null, _keepTeam.KeeperShirt);
        Ui.Text(sb, _keeper.Name, new Vector2(k.X + 14, k.Y + 12), Color.White, 0.9f);
        Ui.Text(sb, $"Brankář {_keepTeam.Code}  -  {_keeper.Tier}", new Vector2(k.X + 14, k.Y + 40), Ui.Muted, 0.7f);
        Widgets.StatBar(sb, new Vector2(k.X + 14, k.Y + 68), k.Width - 28, "Chytání", _keeper.Rating);

        // kdo je na řadě
        string who = _playerShoots ? "KOPEŠ TY" : "CHYTÁŠ TY";
        Ui.Text(sb, who, new Vector2(24, 24), _playerShoots ? Ui.Accent : new Color(90, 170, 255), 0.55f, title: true);
    }

    private void DrawIntro(SpriteBatch sb)
    {
        float a = MathHelper.Clamp(_phaseTime / 0.3f, 0, 1);
        string top = _playerShoots ? "NA ZNAČCE" : "KOPE SOUPEŘ";
        Ui.Text(sb, top, new Vector2(Ui.Width / 2f, 250), Color.White * a, 0.9f, Align.Center);
        Ui.TextCentered(sb, _kicker.Name.ToUpperInvariant(), new Vector2(Ui.Width / 2f, 320), Ui.Accent * a, 0.9f);
    }

    private void DrawAim(SpriteBatch sb)
    {
        float w = ShotResolver.GoalHalfWidth, h = ShotResolver.GoalHeight;

        // obrys brány pro orientaci
        var c0 = Proj(new Vector3(-w, 0, 0));
        var c1 = Proj(new Vector3(-w, h, 0));
        var c2 = Proj(new Vector3(w, h, 0));
        var c3 = Proj(new Vector3(w, 0, 0));
        var frameCol = Color.White * 0.25f;
        Ui.Line(sb, c0, c1, frameCol, 2);
        Ui.Line(sb, c1, c2, frameCol, 2);
        Ui.Line(sb, c2, c3, frameCol, 2);

        float y = _phase == Phase.AimX ? 1.2f : _aimY;
        var pos = Proj(new Vector3(_aimX, y, 0));

        if (_phase == Phase.AimX)
        {
            var a = Proj(new Vector3(-ShotResolver.AimRangeX, 1.2f, 0));
            var b = Proj(new Vector3(ShotResolver.AimRangeX, 1.2f, 0));
            Ui.Line(sb, a, b, Ui.Accent * 0.6f, 3);
        }
        else
        {
            var a = Proj(new Vector3(_aimX, ShotResolver.AimMinY, 0));
            var b = Proj(new Vector3(_aimX, ShotResolver.AimMaxY, 0));
            Ui.Line(sb, a, b, Ui.Accent * 0.6f, 3);
        }

        // kroužek = očekávaný rozptyl (podle Umístění hráče)
        float spread = ShotResolver.Spread(_kicker, MathF.Min(_power, ShotResolver.SweetPowerMax));
        var edge = Proj(new Vector3(_aimX + spread, y, 0));
        float radius = MathF.Max(12, MathF.Abs(edge.X - pos.X));
        bool onTarget = MathF.Abs(_aimX) < w && y < h;
        var col = onTarget ? Ui.Accent : Ui.MissRed;

        Ui.RingAt(sb, pos, radius, col * 0.9f);
        Ui.Dot(sb, pos, 5, col);
        Ui.Line(sb, pos - new Vector2(radius + 8, 0), pos - new Vector2(radius - 6, 0), col, 2);
        Ui.Line(sb, pos + new Vector2(radius - 6, 0), pos + new Vector2(radius + 8, 0), col, 2);
        Ui.Line(sb, pos - new Vector2(0, radius + 8), pos - new Vector2(0, radius - 6), col, 2);
        Ui.Line(sb, pos + new Vector2(0, radius - 6), pos + new Vector2(0, radius + 8), col, 2);

        // kroky 1-2-3
        string[] steps = { "SMĚR", "VÝŠKA", "SÍLA" };
        int current = _phase == Phase.AimX ? 0 : _phase == Phase.AimY ? 1 : 2;
        for (int i = 0; i < 3; i++)
        {
            var r = new Rectangle(Ui.Width / 2 - 165 + i * 112, 112, 106, 30);
            Ui.Rect(sb, r, i == current ? Ui.Accent : i < current ? Ui.GoalGreen * 0.8f : Color.Black * 0.5f);
            Ui.Text(sb, $"{i + 1}. {steps[i]}", new Vector2(r.Center.X, r.Y + 4), i == current ? Color.Black : Color.White, 0.72f, Align.Center, shadow: false);
        }
    }

    private void DrawPowerBar(SpriteBatch sb)
    {
        var r = new Rectangle(Ui.Width - 90, 190, 44, 340);
        Ui.Panel(sb, new Rectangle(r.X - 12, r.Y - 44, r.Width + 24, r.Height + 60));
        Ui.Text(sb, "SÍLA", new Vector2(r.Center.X, r.Y - 34), Color.White, 0.7f, Align.Center);

        int Y(float v) => r.Bottom - (int)(v * r.Height);
        Ui.Rect(sb, new Rectangle(r.X, Y(ShotResolver.SweetPowerMin), r.Width, Y(0) - Y(ShotResolver.SweetPowerMin)), new Color(230, 190, 50));
        Ui.Rect(sb, new Rectangle(r.X, Y(ShotResolver.SweetPowerMax), r.Width, Y(ShotResolver.SweetPowerMin) - Y(ShotResolver.SweetPowerMax)), Ui.GoalGreen);
        Ui.Rect(sb, new Rectangle(r.X, Y(1f), r.Width, Y(ShotResolver.SweetPowerMax) - Y(1f)), Ui.MissRed);
        Ui.Frame(sb, r, Color.White, 2);

        int my = Y(_power);
        Ui.Rect(sb, new Rectangle(r.X - 8, my - 3, r.Width + 16, 6), Color.White);
        Ui.Rect(sb, new Rectangle(r.X - 8, my - 1, r.Width + 16, 2), Color.Black);
    }

    private void DrawDiveHint(SpriteBatch sb)
    {
        var r = new Rectangle(Ui.Width - 540, Ui.Height - 146, 520, 96);
        Ui.Panel(sb, r, null, new Color(90, 170, 255));
        if (_playerDive.HasValue)
        {
            string dir = _playerDive.Value switch { -1 => "VLEVO", 1 => "VPRAVO", _ => "NA STŘED" };
            Ui.Text(sb, $"Skočíš {dir}!", new Vector2(r.Center.X, r.Y + 30), Ui.Accent, 1.1f, Align.Center);
        }
        else
        {
            Ui.Text(sb, "Hádej, kam kope!", new Vector2(r.Center.X, r.Y + 12), Color.White, 1f, Align.Center);
            Ui.Text(sb, "A / šipka vlevo  -  W / šipka nahoru (střed)  -  D / šipka vpravo",
                new Vector2(r.Center.X, r.Y + 46), Ui.Muted, 0.7f, Align.Center);
            Ui.Text(sb, "Nic = brankář chytá sám", new Vector2(r.Center.X, r.Y + 68), Ui.Muted, 0.65f, Align.Center);
        }
    }

    private void DrawBanner(SpriteBatch sb)
    {
        if (_banner == null) return;
        float t = _bannerTime;
        float pop = t < 0.18f ? MathHelper.Lerp(0.3f, 1.25f, t / 0.18f) : MathHelper.Lerp(1.25f, 1.0f, MathHelper.Clamp((t - 0.18f) / 0.2f, 0, 1));
        Ui.TextCentered(sb, _banner, new Vector2(Ui.Width / 2f, 300), _bannerColor, 1.35f * pop);
    }
}
