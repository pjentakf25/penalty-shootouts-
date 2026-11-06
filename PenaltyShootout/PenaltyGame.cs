using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PenaltyShootout.Core;
using PenaltyShootout.Data;
using PenaltyShootout.Rendering;
using PenaltyShootout.Scenes;
using PenaltyShootout.UI;
using PenaltyShootout.World;

namespace PenaltyShootout;

/// <summary>Vstupní bod hry – drží sdílené služby (renderer, svět, zvuk, scény).</summary>
public sealed class PenaltyGame : Game
{
    private readonly GraphicsDeviceManager _graphics;

    public SpriteBatch SpriteBatch { get; private set; }
    public Renderer3D Renderer { get; private set; }
    public World3D World { get; private set; }
    public AudioBank Audio { get; private set; }
    public SceneManager Scenes { get; } = new();
    public float Time { get; private set; }

    private ScreenshotTour _tour;

    public PenaltyGame()
    {
        var args = Environment.GetCommandLineArgs();
        int idx = Array.IndexOf(args, "--screenshots");
        if (idx >= 0 && idx + 1 < args.Length) _tour = new ScreenshotTour(args[idx + 1]);

        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = 1280,
            PreferredBackBufferHeight = 720,
            GraphicsProfile = GraphicsProfile.HiDef,
            PreferMultiSampling = true,
            SynchronizeWithVerticalRetrace = true,
            HardwareModeSwitch = false,
        };
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.AllowUserResizing = true;
        Window.Title = "Penaltový rozstřel 3D";
    }

    protected override void LoadContent()
    {
        SpriteBatch = new SpriteBatch(GraphicsDevice);
        Ui.Load(GraphicsDevice, Content);
        Ball.Load(GraphicsDevice);
        Renderer = new Renderer3D(GraphicsDevice);
        World = new World3D(GraphicsDevice);
        Audio = new AudioBank();
        SetArena(Arena.StadiumDay, new Color(205, 25, 35), new Color(28, 42, 102));
        Scenes.Change(new MainMenuScene(this));
    }

    /// <summary>Postaví prostředí a nastaví podle něj světla a zvuk davu.</summary>
    public void SetArena(Arena arena, Color homeColor, Color awayColor)
    {
        World.Build(arena, homeColor, awayColor);
        Renderer.ApplyStyle(World.Style);
        Audio.SetCrowd(World.Style.CrowdVolume);
    }

    protected override void Update(GameTime gameTime)
    {
        float dt = MathF.Min((float)gameTime.ElapsedGameTime.TotalSeconds, 1f / 20f);
        Time += dt;
        Input.Update(IsActive);

        if (Input.Pressed(Keys.F11) || (Input.Down(Keys.LeftAlt) && Input.Pressed(Keys.Enter)))
            ToggleFullscreen();

        if (World.Style.Snow) World.Snow.Update(dt);
        _tour?.Update(this, dt);
        Scenes.Update(dt);
        base.Update(gameTime);
    }

    private void ToggleFullscreen()
    {
        if (_graphics.IsFullScreen)
        {
            _graphics.IsFullScreen = false;
            _graphics.PreferredBackBufferWidth = 1280;
            _graphics.PreferredBackBufferHeight = 720;
        }
        else
        {
            var mode = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
            _graphics.PreferredBackBufferWidth = mode.Width;
            _graphics.PreferredBackBufferHeight = mode.Height;
            _graphics.IsFullScreen = true;
        }
        _graphics.ApplyChanges();
    }

    protected override void Draw(GameTime gameTime)
    {
        Ui.UpdateViewport(GraphicsDevice.Viewport);
        Scenes.Draw((float)gameTime.ElapsedGameTime.TotalSeconds);
        _tour?.AfterDraw(this);
        base.Draw(gameTime);
    }

    /// <summary>
    /// Vykreslí oblohu, prostředí, postavy (opaque i průhledné části) a sníh.
    /// UI si každá scéna kreslí sama až potom.
    /// </summary>
    public void DrawWorld(Action<Renderer3D> opaque = null, Action<Renderer3D> transparent = null)
    {
        var s = World.Style;
        GraphicsDevice.Clear(s.SkyBottom);
        Ui.Sky(SpriteBatch, GraphicsDevice.Viewport, s.SkyTop, s.SkyBottom, s.Night);

        Renderer.Begin();
        World.DrawOpaque(Renderer);
        opaque?.Invoke(Renderer);
        transparent?.Invoke(Renderer);
        World.DrawTransparent(Renderer);

        if (s.Snow)
        {
            Ui.Begin(SpriteBatch);
            World.Snow.Draw(SpriteBatch);
            SpriteBatch.End();
        }
    }

    /// <summary>Kamera obíhající kolem hřiště (pozadí menu).</summary>
    public void OrbitCamera(float speed = 0.07f, float radius = 30f, float height = 9f)
    {
        float a = Time * speed + 0.6f;
        var c = Renderer.Camera;
        c.Position = new Vector3(MathF.Sin(a) * radius, height, 12f + MathF.Cos(a) * radius);
        c.Target = new Vector3(0, 1.5f, 6f);
    }
}
