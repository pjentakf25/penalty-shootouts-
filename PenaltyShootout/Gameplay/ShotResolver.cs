using System;
using Microsoft.Xna.Framework;
using PenaltyShootout.Core;
using PenaltyShootout.Data;

namespace PenaltyShootout.Gameplay;

public enum ShotOutcome { Goal, PostIn, Saved, Post, Wide, Over }

public static class ShotOutcomeExtensions
{
    public static bool IsGoal(this ShotOutcome o) => o is ShotOutcome.Goal or ShotOutcome.PostIn;
}

/// <summary>Kam a jak rychle míč poletí.</summary>
public struct ShotPlan
{
    /// <summary>Bod v rovině brány (x = do stran, y = výška).</summary>
    public Vector2 Target;
    public float Speed;          // m/s
    public float SpeedNorm;      // 0..1
    public bool Panenka;         // vršovaný lob doprostřed
}

/// <summary>
/// Pravidla penalty: rozměry brány, rozptyl střely podle Umístění,
/// rychlost podle Razance a šance brankáře na zákrok podle jeho ratingu.
/// </summary>
public static class ShotResolver
{
    public const float GoalHalfWidth = 3.66f;
    public const float GoalHeight = 2.44f;
    public const float PostThickness = 0.12f;
    public const float BallRadius = 0.11f;
    public const float SpotDistance = 11f;

    /// <summary>Rozsah zaměřovače do stran (o kousek za tyče, takže jde minout).</summary>
    public const float AimRangeX = 4.4f;
    public const float AimMinY = 0.15f;
    public const float AimMaxY = 2.95f;

    public const float SweetPowerMin = 0.40f;
    public const float SweetPowerMax = 0.86f;

    public static float Accuracy01(Kicker k) => k.Accuracy / 100f;
    public static float Power01(Kicker k) => k.Power / 100f;

    /// <summary>Rozptyl střely v metrech – čím horší umístění, tím víc míč uteče.</summary>
    public static float Spread(Kicker k, float power)
    {
        float over = MathF.Max(0, power - SweetPowerMax) / (1f - SweetPowerMax);
        return MathHelper.Lerp(0.6f, 0.07f, Accuracy01(k)) * (1f + over * 1.6f);
    }

    /// <summary>Rychlost ukazatelů – horší hráč = rychlejší a hůř se trefuje.</summary>
    public static float AimSpeed(Kicker k) => 2.3f * MathHelper.Lerp(1.5f, 0.85f, Accuracy01(k));
    public static float PowerSpeed(Kicker k) => 1.15f * MathHelper.Lerp(1.35f, 0.9f, Power01(k));

    public static ShotPlan Build(Kicker k, Vector2 aim, float power)
    {
        float acc = Accuracy01(k);
        float over = MathF.Max(0, power - SweetPowerMax) / (1f - SweetPowerMax);
        float sigma = Spread(k, power);

        var plan = new ShotPlan();
        plan.Panenka = power < 0.22f && MathF.Abs(aim.X) < 1.2f && aim.Y > 0.9f;

        float tx = aim.X + Rng.Gauss() * sigma;
        float ty = aim.Y + Rng.Gauss() * sigma * 0.6f + over * MathHelper.Lerp(1.3f, 0.6f, acc);
        plan.Target = new Vector2(tx, MathF.Max(BallRadius, ty));

        plan.Speed = plan.Panenka
            ? 8.5f
            : MathHelper.Lerp(11f, 31f, power) * MathHelper.Lerp(0.78f, 1.08f, Power01(k));
        plan.SpeedNorm = MathHelper.Clamp((plan.Speed - 10f) / 22f, 0, 1);
        return plan;
    }

    /// <summary>AI exekutor soupeře – vybere roh podle sebe a vystřelí.</summary>
    public static ShotPlan BuildAi(Kicker k)
    {
        float acc = Accuracy01(k);
        Vector2 aim;
        if (Rng.Chance(0.14f))
            aim = new Vector2(Rng.Range(-0.8f, 0.8f), Rng.Range(0.3f, 1.8f));
        else
        {
            // lepší střelec míří blíž k tyči
            float x = MathHelper.Lerp(1.6f, 3.15f, MathF.Pow(Rng.Value, 0.6f) * (0.55f + 0.45f * acc));
            aim = new Vector2(Rng.Sign * x, Rng.Range(0.25f, 2.05f));
        }
        float power = Rng.Range(0.5f, 0.93f);
        return Build(k, aim, power);
    }

    /// <summary>0..1 – kvalita brankáře.</summary>
    public static float KeeperFactor(int rating) => MathHelper.Clamp((rating - 45f) / 50f, 0f, 1f);

    /// <summary>
    /// Pravděpodobnost zákroku na střelu mířící do brány.
    /// Top brankář chytí většinu, ale ne všechno; slabý skoro nic.
    /// </summary>
    public static float SaveChance(int rating, Vector2 target, float speedNorm)
    {
        float r = KeeperFactor(rating);
        float dx = MathHelper.Clamp(MathF.Abs(target.X) / GoalHalfWidth, 0, 1);
        float dy = MathHelper.Clamp(target.Y / GoalHeight, 0, 1);
        float placement = MathHelper.Clamp(0.65f * dx + 0.35f * dy * dx + 0.2f * dy, 0, 1); // rohy = těžké
        float baseChance = MathHelper.Lerp(0.03f, 0.93f, MathF.Pow(r, 1.5f));
        return baseChance * (1f - 0.32f * placement * placement) * (1f - 0.12f * speedNorm);
    }

    public static float PanenkaSaveChance(int rating) => 0.2f + 0.4f * KeeperFactor(rating);

    /// <summary>-1 = vlevo, 0 = střed, 1 = vpravo (z pohledu střelce).</summary>
    public static int Zone(float x) => x < -1.1f ? -1 : x > 1.1f ? 1 : 0;

    /// <summary>Výsledek podle toho, kam míč míří vzhledem k rámu brány (bez brankáře).</summary>
    public static ShotOutcome? Frame(Vector2 t)
    {
        float ax = MathF.Abs(t.X);
        float inner = GoalHalfWidth - BallRadius;
        float outer = GoalHalfWidth + PostThickness + BallRadius;

        if (ax <= inner && t.Y <= GoalHeight - BallRadius) return null; // jde na bránu

        bool hitsPost = ax > inner && ax < outer && t.Y < GoalHeight + PostThickness;
        bool hitsBar = t.Y > GoalHeight - BallRadius && t.Y < GoalHeight + PostThickness + BallRadius && ax < outer;
        if (hitsPost || hitsBar) return Rng.Chance(0.35f) ? ShotOutcome.PostIn : ShotOutcome.Post;

        return t.Y > GoalHeight ? ShotOutcome.Over : ShotOutcome.Wide;
    }

    public static string ShortLabel(ShotOutcome o) => o switch
    {
        ShotOutcome.Goal => "GÓL",
        ShotOutcome.PostIn => "TYČ A GÓL",
        ShotOutcome.Saved => "CHYCENO",
        ShotOutcome.Post => "TYČ",
        ShotOutcome.Over => "PŘES BRÁNU",
        _ => "VEDLE",
    };

    public static string Banner(ShotOutcome o, bool panenka) => o switch
    {
        ShotOutcome.Goal => panenka ? "PANENKA!" : "GÓÓÓL!",
        ShotOutcome.PostIn => "TYČ A GÓL!",
        ShotOutcome.Saved => "CHYCENO!",
        ShotOutcome.Post => "TYČ!",
        ShotOutcome.Over => "PŘES BRÁNU!",
        _ => "VEDLE!",
    };
}
