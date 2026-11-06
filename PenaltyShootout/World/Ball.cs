using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PenaltyShootout.Gameplay;
using PenaltyShootout.Rendering;

namespace PenaltyShootout.World;

/// <summary>
/// Míč: let k bráně je „naskriptovaný“ (přesně tam, kam určila střela),
/// potom přebírá jednoduchá fyzika (gravitace, odrazy, síť).
/// </summary>
public sealed class Ball
{
    public const float Radius = ShotResolver.BallRadius;
    public static readonly Vector3 Spot = new(0, Radius, ShotResolver.SpotDistance);

    private static Mesh _mesh;

    public Vector3 Position;
    public Vector3 Velocity;
    public Matrix Rotation = Matrix.Identity;

    public bool Scripted { get; private set; }
    public bool ScriptDone { get; private set; }
    /// <summary>Míč je v bráně – zastaví ho síť.</summary>
    public bool InNet;
    public bool Physics { get; private set; }

    private Vector3 _from, _to;
    private float _duration, _time, _arc, _curve;

    public static void Load(GraphicsDevice gd)
    {
        if (_mesh != null) return;
        var b = new MeshBuilder();
        b.AddSphere(Matrix.Identity, 8, 12, (r, s) =>
            (r + s) % 3 == 0 && r > 0 && r < 7 ? new Color(25, 25, 30) : Color.White);
        _mesh = b.Build(gd);
    }

    public void Reset()
    {
        Position = Spot;
        Velocity = Vector3.Zero;
        Scripted = ScriptDone = InNet = Physics = false;
    }

    public void Launch(Vector3 to, float duration, float arc, float curve)
    {
        _from = Position;
        _to = to;
        _duration = MathF.Max(0.1f, duration);
        _time = 0;
        _arc = arc;
        _curve = curve;
        Scripted = true;
        ScriptDone = false;
        Physics = false;
    }

    private Vector3 Eval(float u) =>
        Vector3.Lerp(_from, _to, u) + new Vector3(_curve * MathF.Sin(MathF.PI * u), _arc * 4f * u * (1 - u), 0);

    /// <summary>Po doletu skriptu pokračovat fyzikou s danou rychlostí.</summary>
    public void Release(Vector3 velocity)
    {
        Velocity = velocity;
        Physics = true;
    }

    public void Update(float dt)
    {
        var prev = Position;

        if (Scripted)
        {
            _time += dt;
            float u = MathF.Min(1f, _time / _duration);
            Position = Eval(u);
            float u0 = MathF.Max(0, u - 0.02f);
            Velocity = (Eval(u) - Eval(u0)) / MathF.Max(1e-4f, (u - u0) * _duration);
            if (u >= 1f)
            {
                Scripted = false;
                ScriptDone = true;
                Physics = true;
            }
        }
        else if (Physics)
        {
            Velocity.Y -= 9.81f * dt;
            Position += Velocity * dt;

            if (Position.Y < Radius)
            {
                Position.Y = Radius;
                Velocity.Y = Velocity.Y < -0.6f ? -Velocity.Y * 0.5f : 0f;
                float drag = MathF.Max(0, 1f - 1.4f * dt);
                Velocity.X *= drag;
                Velocity.Z *= drag;
            }

            if (InNet) CollideNet();

            // reklamní panely za brankou
            if (Position.Z < -3.4f && Velocity.Z < 0)
            {
                Velocity.Z = -Velocity.Z * 0.35f;
                Velocity.X *= 0.6f;
            }
        }

        // rotace míče podle uražené dráhy
        var d = Position - prev;
        float dist = d.Length();
        if (dist > 1e-5f)
        {
            var axis = Vector3.Cross(Vector3.Up, d / dist);
            if (axis.LengthSquared() > 1e-6f)
            {
                axis.Normalize();
                Rotation *= Matrix.CreateFromAxisAngle(axis, dist / Radius);
            }
        }
    }

    private void CollideNet()
    {
        float h = ShotResolver.GoalHeight;
        float w = ShotResolver.GoalHalfWidth;
        float backZ = -0.9f - (h - MathHelper.Clamp(Position.Y, 0, h)) / h * 1.1f;

        if (Position.Z < backZ + Radius)
        {
            Position.Z = backZ + Radius;
            Velocity.Z = MathF.Abs(Velocity.Z) * 0.06f;
            Velocity.X *= 0.3f;
            Velocity.Y = MathF.Min(Velocity.Y, 0) * 0.3f;
        }
        if (MathF.Abs(Position.X) > w - Radius && Position.Z < 0)
        {
            Position.X = MathF.Sign(Position.X) * (w - Radius);
            Velocity.X *= -0.2f;
        }
        if (Position.Y > h - Radius && Position.Z < 0)
        {
            Position.Y = h - Radius;
            Velocity.Y = MathF.Min(0, Velocity.Y);
        }
    }

    public void Draw(Renderer3D r)
    {
        r.Draw(_mesh, Matrix.CreateScale(Radius * 2f) * Rotation * Matrix.CreateTranslation(Position));
    }

    public void DrawShadow(Renderer3D r) =>
        r.DrawShadow(Position, 0.13f, 0.4f / (1f + Position.Y * 0.8f));
}
