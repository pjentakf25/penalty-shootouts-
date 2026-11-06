using System;
using Microsoft.Xna.Framework;
using PenaltyShootout.Core;

namespace PenaltyShootout.UI;

/// <summary>Výběr položky šipkami / WASD / myší. Update vrátí true při potvrzení.</summary>
public sealed class SelectionGrid
{
    public int Count;
    public int Columns = 1;
    public int Selected;
    public Func<int, Rectangle> RectFor;
    public Func<int, bool> IsEnabled = _ => true;
    public Action OnMove;

    public bool Update()
    {
        if (Count <= 0) return false;
        int before = Selected;

        if (Columns > 1)
        {
            if (Input.Left) Step(-1);
            if (Input.Right) Step(1);
            if (Input.Up) Step(-Columns);
            if (Input.DownKey) Step(Columns);
        }
        else
        {
            if (Input.Up || Input.Left) Step(-1);
            if (Input.DownKey || Input.Right) Step(1);
        }

        bool confirmed = false;
        if (RectFor != null)
        {
            var m = Input.MousePosition;
            for (int i = 0; i < Count; i++)
            {
                if (!RectFor(i).Contains(m) || !IsEnabled(i)) continue;
                if (Input.MouseMoved) Selected = i;
                if (Input.LeftClick) { Selected = i; confirmed = true; }
            }
        }

        if (Selected != before) OnMove?.Invoke();
        if (!IsEnabled(Selected)) Step(1);

        return confirmed || Input.Pressed(Microsoft.Xna.Framework.Input.Keys.Enter)
                         || Input.Pressed(Microsoft.Xna.Framework.Input.Keys.Space);
    }

    private void Step(int delta)
    {
        int idx = Selected;
        for (int tries = 0; tries < Count; tries++)
        {
            idx = ((idx + delta) % Count + Count) % Count;
            if (IsEnabled(idx)) { Selected = idx; return; }
            if (Math.Abs(delta) > 1) delta = Math.Sign(delta); // u mřížky přeskoč na další povolenou
        }
    }
}
