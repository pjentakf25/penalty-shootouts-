using System;
using Microsoft.Xna.Framework;
using PenaltyShootout.World;

namespace PenaltyShootout.Gameplay;

/// <summary>
/// Brankář: přešlapuje na čáře, při kopu skočí tak, aby rukavice
/// doletěly na zadaný bod v rovině brány (x, y).
/// </summary>
public sealed class KeeperController
{
    public const float LineZ = 0.45f;
    private const float Reach = 1.35f; // vzdálenost kyčle → rukavice při vzpažení

    public Humanoid Model { get; }

    private float _idle;
    private bool _diving;
    private float _diveT, _delay, _duration;
    private Vector3 _startPos, _targetRoot;
    private float _targetRoll, _targetRaise, _targetCrouch;
    private bool _side;
    private bool _celebrate;

    public KeeperController(Appearance look)
    {
        Model = new Humanoid(look) { Yaw = 0 };
        Model.ResetPose();
        Model.Position = new Vector3(0, 0, LineZ);
    }

    /// <summary>Skok na bod hands (rovina brány) se zpožděním delay, trvání duration.</summary>
    public void StartDive(Vector2 hands, float delay, float duration)
    {
        _diving = true;
        _diveT = 0;
        _delay = delay;
        _duration = MathF.Max(0.22f, duration);
        _startPos = Model.Position;

        if (MathF.Abs(hands.X) < 0.9f)
        {
            // zákrok na střed – výskok nebo podřep
            _side = false;
            _targetRoll = 0;
            _targetRoot = new Vector3(hands.X * 0.85f, MathF.Max(0, hands.Y - 2.05f), LineZ);
            _targetRaise = hands.Y > 1.25f ? 2.9f : 0.5f;
            _targetCrouch = hands.Y < 0.7f ? 0.35f : 0f;
        }
        else
        {
            _side = true;
            float s = MathF.Sign(hands.X);
            float theta = MathHelper.Clamp(MathF.Atan2(MathF.Abs(hands.X), hands.Y - 0.55f), 0.35f, 1.55f);
            var dir = new Vector2(s * MathF.Sin(theta), MathF.Cos(theta));
            var hip = hands - dir * Reach;
            hip.Y = MathF.Max(hip.Y, 0.3f);
            _targetRoot = new Vector3(hip.X, hip.Y - 0.9f, LineZ);
            _targetRoll = -s * theta;
            _targetRaise = 2.95f;
            _targetCrouch = 0f;
        }
    }

    public void Celebrate() => _celebrate = true;

    public void Update(float dt)
    {
        _idle += dt;
        var m = Model;

        if (!_diving)
        {
            m.ResetPose();
            m.Position = new Vector3(MathF.Sin(_idle * 1.7f) * 0.22f, 0, LineZ);
            m.Crouch = 0.14f + 0.04f * MathF.Sin(_idle * 6f);
            m.Pitch = 0.15f;
            m.RaiseL = m.RaiseR = 0.6f;
            m.ArmL = m.ArmR = 0.35f;
            m.LegL = 0.1f * MathF.Sin(_idle * 3.4f);
            m.LegR = -m.LegL;
            return;
        }

        _diveT += dt;
        float tau = _diveT - _delay;
        if (tau < 0)
        {
            m.Crouch = 0.22f;
            return;
        }

        float u = MathHelper.Clamp(tau / _duration, 0, 1);
        float e = 1 - (1 - u) * (1 - u);

        m.Position = Vector3.Lerp(_startPos, _targetRoot, e);
        if (_side) m.Position.Y += MathF.Sin(MathF.PI * u) * 0.25f;
        m.Roll = MathHelper.Lerp(0, _targetRoll, e);
        float armU = MathHelper.Clamp(u * 1.7f, 0, 1);
        m.RaiseL = m.RaiseR = MathHelper.Lerp(0.6f, _targetRaise, armU);
        m.ArmL = m.ArmR = MathHelper.Lerp(0.35f, _side ? 0.1f : 0.6f, armU);
        m.Crouch = MathHelper.Lerp(0.14f, _targetCrouch, e);
        m.Pitch = MathHelper.Lerp(0.15f, 0f, e);
        m.LegL = _side ? 0.35f * e : 0;
        m.LegR = _side ? -0.2f * e : 0;

        if (u < 1f) return;

        float after = tau - _duration;
        if (_side)
        {
            // dopad na trávník
            float v = MathHelper.Clamp(after / 0.3f, 0, 1);
            float s = MathF.Sign(_targetRoll);
            m.Position.Y = MathHelper.Lerp(_targetRoot.Y, 0.28f - 0.9f, v * v);
            m.Roll = MathHelper.Lerp(_targetRoll, s * 1.5f, v);
            if (_celebrate && after > 0.9f)
            {
                float g = MathHelper.Clamp((after - 0.9f) / 0.4f, 0, 1);
                m.Roll = MathHelper.Lerp(m.Roll, 0, g);
                m.Position.Y = MathHelper.Lerp(m.Position.Y, 0, g);
                m.RaiseL = m.RaiseR = 2.8f;
            }
        }
        else
        {
            float v = MathHelper.Clamp(after / 0.4f, 0, 1);
            m.Position.Y = MathHelper.Lerp(_targetRoot.Y, 0, v);
            m.Crouch = MathHelper.Lerp(_targetCrouch, 0, v);
            if (_celebrate && after > 0.5f)
            {
                m.RaiseL = m.RaiseR = 2.7f + 0.2f * MathF.Sin(after * 9);
                m.Position.Y = MathF.Abs(MathF.Sin(after * 6f)) * 0.25f;
            }
        }
    }
}
