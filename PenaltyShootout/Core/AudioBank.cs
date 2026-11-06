using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace PenaltyShootout.Core;

/// <summary>
/// Zvuky generované procedurálně (hra nepotřebuje žádné zvukové soubory):
/// píšťalka, kop do míče, tyčka, jásot a „óóó“ publika, šum stadionu.
/// </summary>
public sealed class AudioBank
{
    private const int SampleRate = 22050;

    private SoundEffect _whistle, _finalWhistle, _kick, _cheer, _groan, _post, _click, _crowdLoop;
    private SoundEffectInstance _crowd;

    public bool Enabled { get; private set; }
    public float Master { get; set; } = 0.8f;

    public AudioBank()
    {
        try
        {
            var rnd = new Random(7);
            float Noise() => rnd.NextSingle() * 2f - 1f;

            _whistle = Generate(0.45f, t =>
            {
                float env = MathHelper.Clamp(t / 0.02f, 0, 1) * MathHelper.Clamp((0.45f - t) / 0.06f, 0, 1);
                return MathF.Sin(2 * MathF.PI * 2900 * t + 6 * MathF.Sin(2 * MathF.PI * 34 * t)) * 0.35f * env;
            });

            _finalWhistle = Generate(1.4f, t =>
            {
                // tři hvizdy – konec zápasu
                float local = t % 0.47f;
                float env = MathHelper.Clamp(local / 0.02f, 0, 1) * MathHelper.Clamp((0.40f - local) / 0.05f, 0, 1);
                return MathF.Sin(2 * MathF.PI * 2850 * t + 6 * MathF.Sin(2 * MathF.PI * 34 * t)) * 0.35f * env;
            });

            _kick = Generate(0.18f, t =>
                MathF.Sin(2 * MathF.PI * (120 - 260 * t) * t) * MathF.Exp(-t * 28) * 0.9f
                + Noise() * MathF.Exp(-t * 90) * 0.5f);

            float lp1 = 0, lp2 = 0;
            _cheer = Generate(3.0f, t =>
            {
                lp1 += (Noise() - lp1) * 0.30f;
                lp2 += (lp1 - lp2) * 0.45f;
                float env = MathHelper.Clamp(t / 0.25f, 0, 1) * MathF.Exp(-MathF.Max(0, t - 0.7f) * 1.3f);
                float wobble = 0.8f + 0.2f * MathF.Sin(2 * MathF.PI * 3.1f * t);
                return lp2 * 1.6f * env * wobble;
            });

            float lp3 = 0;
            _groan = Generate(2.2f, t =>
            {
                lp3 += (Noise() - lp3) * 0.06f;
                float env = MathHelper.Clamp(t / 0.3f, 0, 1) * MathF.Exp(-MathF.Max(0, t - 0.6f) * 1.6f);
                float voice = MathF.Sin(2 * MathF.PI * (170 - 35 * t) * t) * 0.12f;
                return (lp3 * 2.2f + voice) * env;
            });

            _post = Generate(0.7f, t =>
                (MathF.Sin(2 * MathF.PI * 1250 * t) + 0.6f * MathF.Sin(2 * MathF.PI * 2730 * t)
                 + 0.4f * MathF.Sin(2 * MathF.PI * 610 * t)) * MathF.Exp(-t * 7) * 0.3f);

            _click = Generate(0.05f, t => MathF.Sin(2 * MathF.PI * 1300 * t) * MathF.Exp(-t * 90) * 0.35f);

            float lp4 = 0;
            _crowdLoop = Generate(4f, t =>
            {
                lp4 += (Noise() - lp4) * 0.12f;
                float fade = MathHelper.Clamp(t / 0.05f, 0, 1) * MathHelper.Clamp((4f - t) / 0.05f, 0, 1);
                return lp4 * 0.9f * (0.85f + 0.15f * MathF.Sin(2 * MathF.PI * 0.5f * t)) * fade;
            });

            _crowd = _crowdLoop.CreateInstance();
            _crowd.IsLooped = true;
            Enabled = true;
        }
        catch (Exception)
        {
            Enabled = false; // bez zvukového zařízení hra běží dál potichu
        }
    }

    private static SoundEffect Generate(float seconds, Func<float, float> sample)
    {
        int n = (int)(seconds * SampleRate);
        var buffer = new byte[n * 2];
        for (int i = 0; i < n; i++)
        {
            float v = MathHelper.Clamp(sample(i / (float)SampleRate), -1f, 1f);
            short s = (short)(v * short.MaxValue);
            buffer[i * 2] = (byte)(s & 0xff);
            buffer[i * 2 + 1] = (byte)((s >> 8) & 0xff);
        }
        return new SoundEffect(buffer, SampleRate, AudioChannels.Mono);
    }

    private void Play(SoundEffect s, float volume)
    {
        if (!Enabled || s == null) return;
        try { s.Play(MathHelper.Clamp(volume * Master, 0, 1), 0, 0); } catch { /* ignorovat */ }
    }

    public void Whistle() => Play(_whistle, 0.6f);
    public void FinalWhistle() => Play(_finalWhistle, 0.6f);
    public void Kick() => Play(_kick, 1f);
    public void Cheer(float volume = 1f) => Play(_cheer, volume);
    public void Groan(float volume = 1f) => Play(_groan, volume);
    public void Post() => Play(_post, 0.8f);
    public void Click() => Play(_click, 0.5f);

    /// <summary>Šum davu na pozadí (na stadionu hlasitější, na hřišti tišší).</summary>
    public void SetCrowd(float volume)
    {
        if (!Enabled) return;
        try
        {
            _crowd.Volume = MathHelper.Clamp(volume * Master, 0, 1);
            if (volume <= 0.001f) { if (_crowd.State == SoundState.Playing) _crowd.Pause(); }
            else if (_crowd.State != SoundState.Playing) _crowd.Play();
        }
        catch { /* ignorovat */ }
    }
}
