using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PenaltyShootout.UI;

namespace PenaltyShootout.World;

/// <summary>Padající sníh – jednoduché 2D vločky přes obraz.</summary>
public sealed class Snowfall
{
    private struct Flake { public Vector2 Pos; public float Speed, Size, Phase; }

    private readonly Flake[] _flakes = new Flake[380];
    private float _time;

    public Snowfall()
    {
        var rnd = new Random(99);
        for (int i = 0; i < _flakes.Length; i++)
        {
            _flakes[i] = new Flake
            {
                Pos = new Vector2((float)rnd.NextDouble() * Ui.Width, (float)rnd.NextDouble() * Ui.Height),
                Speed = 30 + (float)rnd.NextDouble() * 70,
                Size = 1.5f + (float)rnd.NextDouble() * 3f,
                Phase = (float)rnd.NextDouble() * 10,
            };
        }
    }

    public void Update(float dt)
    {
        _time += dt;
        for (int i = 0; i < _flakes.Length; i++)
        {
            ref var f = ref _flakes[i];
            f.Pos.Y += f.Speed * dt;
            f.Pos.X += MathF.Sin(_time * 1.3f + f.Phase) * 18f * dt + 8f * dt;
            if (f.Pos.Y > Ui.Height + 10) { f.Pos.Y = -10; }
            if (f.Pos.X > Ui.Width + 10) f.Pos.X = -10;
        }
    }

    public void Draw(SpriteBatch sb)
    {
        foreach (var f in _flakes)
            Ui.Dot(sb, f.Pos, f.Size, Color.White * 0.85f);
    }
}
