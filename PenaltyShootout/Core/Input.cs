using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PenaltyShootout.UI;

namespace PenaltyShootout.Core;

/// <summary>Stav klávesnice a myši za aktuální a předchozí snímek.</summary>
public static class Input
{
    private static KeyboardState _kb, _prevKb;
    private static MouseState _ms, _prevMs;

    public static void Update(bool active)
    {
        _prevKb = _kb;
        _prevMs = _ms;
        _kb = active ? Keyboard.GetState() : default;
        _ms = active ? Mouse.GetState() : _prevMs;
    }

    public static bool Down(Keys k) => _kb.IsKeyDown(k);
    public static bool Pressed(Keys k) => _kb.IsKeyDown(k) && !_prevKb.IsKeyDown(k);

    public static bool LeftClick =>
        _ms.LeftButton == ButtonState.Pressed && _prevMs.LeftButton == ButtonState.Released;

    /// <summary>Pozice myši ve virtuálních souřadnicích UI (1280×720).</summary>
    public static Vector2 MousePosition => Ui.ScreenToVirtual(_ms.Position.ToVector2());

    public static bool MouseMoved => _ms.Position != _prevMs.Position;

    /// <summary>Hlavní herní akce – mezerník, Enter nebo kliknutí.</summary>
    public static bool Action => Pressed(Keys.Space) || Pressed(Keys.Enter) || LeftClick;

    public static bool Back => Pressed(Keys.Escape) || Pressed(Keys.Back);

    public static bool Left => Pressed(Keys.Left) || Pressed(Keys.A);
    public static bool Right => Pressed(Keys.Right) || Pressed(Keys.D);
    public static bool Up => Pressed(Keys.Up) || Pressed(Keys.W);
    public static bool DownKey => Pressed(Keys.Down) || Pressed(Keys.S);
}
