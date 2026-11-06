using System;
using Microsoft.Xna.Framework;
using PenaltyShootout.World;

namespace PenaltyShootout.Gameplay;

public enum Mood { None, Happy, Sad }

/// <summary>Rozběh a kop exekutora (pravá noha), potom radost nebo zklamání.</summary>
public sealed class KickerController
{
    public const float RunDuration = 1.05f;
    public const float ContactTime = RunDuration + 0.17f;

    private static readonly Vector3 Start = new(-1.9f, 0, 13.7f);
    private static readonly Vector3 Plant = new(-0.12f, 0, 11.55f);

    public Humanoid Model { get; }
    public bool Running { get; private set; }
    public bool JustKicked { get; private set; }
    public Mood Mood { get; set; }

    private float _t;      // čas od rozběhu
    private float _idle;
    private float _moodT;

    public KickerController(Appearance look)
    {
        Model = new Humanoid(look);
        Reset();
    }

    public void Reset()
    {
        Running = false;
        Mood = Mood.None;
        _t = 0;
        Model.ResetPose();
        Model.Position = Start;
        Model.Yaw = RunYaw;
    }

    private static float RunYaw
    {
        get
        {
            var d = Plant - Start;
            return MathF.Atan2(d.X, d.Z);
        }
    }

    public void BeginRun()
    {
        Running = true;
        _t = 0;
    }

    public void Update(float dt)
    {
        JustKicked = false;
        _idle += dt;
        var m = Model;

        if (!Running)
        {
            // čeká na značce, lehce se pohupuje
            m.ResetPose();
            m.Position = Start;
            m.Yaw = RunYaw;
            m.Crouch = 0.02f + 0.02f * MathF.Sin(_idle * 2.2f);
            m.ArmL = m.ArmR = 0.05f * MathF.Sin(_idle * 2.2f);
            return;
        }

        float prev = _t;
        _t += dt;
        if (prev < ContactTime && _t >= ContactTime) JustKicked = true;

        if (_t < RunDuration)
        {
            float u = _t / RunDuration;
            float e = u * (0.6f + 0.4f * u);
            m.Position = Vector3.Lerp(Start, Plant, e);
            m.Position.Y = MathF.Abs(MathF.Sin(_t * 14f)) * 0.05f;
            m.Yaw = RunYaw;
            float s = MathF.Sin(_t * 14f);
            m.LegL = s * 0.8f;
            m.LegR = -s * 0.8f;
            m.ArmL = -s * 0.6f;
            m.ArmR = s * 0.6f;
            m.RaiseL = m.RaiseR = 0.2f;
            m.Pitch = 0.15f;
            // poslední krok – nápřah pravé nohy
            if (u > 0.75f) m.LegR = MathHelper.Lerp(m.LegR, -1.0f, (u - 0.75f) / 0.25f);
            return;
        }

        float k = _t - RunDuration;
        m.Position = Plant;
        m.Yaw = MathHelper.Lerp(RunYaw, MathF.PI - 0.08f, MathHelper.Clamp(k / 0.3f, 0, 1));
        m.LegL = -0.15f;
        m.Pitch = 0.12f;
        m.RaiseL = 0.9f;
        m.RaiseR = 0.4f;
        m.ArmR = -0.4f;

        if (k < 0.12f) m.LegR = MathHelper.Lerp(-1.0f, -1.35f, k / 0.12f);
        else if (k < 0.22f) m.LegR = MathHelper.Lerp(-1.35f, 1.35f, (k - 0.12f) / 0.10f);
        else if (k < 0.45f) m.LegR = MathHelper.Lerp(1.35f, 1.6f, (k - 0.22f) / 0.23f);
        else m.LegR = MathHelper.Lerp(1.6f, 0f, MathHelper.Clamp((k - 0.45f) / 0.35f, 0, 1));

        if (k > 0.8f) ApplyMood(dt);
    }

    private void ApplyMood(float dt)
    {
        var m = Model;
        _moodT += dt;
        m.LegR = m.LegL = 0;
        switch (Mood)
        {
            case Mood.Happy:
                m.RaiseL = m.RaiseR = 2.7f + 0.2f * MathF.Sin(_moodT * 9);
                m.Position.Y = MathF.Abs(MathF.Sin(_moodT * 7f)) * 0.3f;
                m.Pitch = -0.1f;
                m.Yaw = MathF.PI + MathF.Sin(_moodT * 2f) * 0.6f;
                break;
            case Mood.Sad:
                m.RaiseL = m.RaiseR = 2.1f;
                m.ArmL = m.ArmR = 0.6f;
                m.Pitch = 0.3f;
                m.Crouch = 0.05f;
                break;
            default:
                m.RaiseL = m.RaiseR = 0.15f;
                m.Pitch = 0f;
                break;
        }
    }
}
