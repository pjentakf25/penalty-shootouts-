using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace PenaltyShootout.Data;

public enum PlayerPosition { Forward, Midfielder, Defender }

/// <summary>Jak nakreslit vlajku z barev v <see cref="Team.Flag"/>.</summary>
public enum FlagStyle { Horizontal, Vertical, Cross, Czech, Brazil }

public static class PlayerPositionExtensions
{
    public static string Czech(this PlayerPosition p) => p switch
    {
        PlayerPosition.Forward => "Útočník",
        PlayerPosition.Midfielder => "Záložník",
        _ => "Obránce",
    };

    public static string Short(this PlayerPosition p) => p switch
    {
        PlayerPosition.Forward => "ÚTO",
        PlayerPosition.Midfielder => "ZÁL",
        _ => "OBR",
    };
}

/// <summary>Hráč, který kope penaltu.</summary>
public sealed class Kicker
{
    public string Name { get; }
    public PlayerPosition Position { get; }

    /// <summary>Razance střely 0–100 (rychlost míče = méně času pro brankáře).</summary>
    public int Power { get; }

    /// <summary>Umístění 0–100 (přesnost – jak moc míč letí tam, kam míříš).</summary>
    public int Accuracy { get; }

    public Kicker(string name, PlayerPosition position, int power, int accuracy)
    {
        Name = name;
        Position = position;
        Power = power;
        Accuracy = accuracy;
    }

    /// <summary>Celkové hodnocení exekutora penalt.</summary>
    public int PenaltySkill => (int)MathF.Round(Power * 0.4f + Accuracy * 0.6f);

    public string ShortName
    {
        get
        {
            int i = Name.LastIndexOf(' ');
            return i < 0 ? Name : Name[(i + 1)..];
        }
    }
}

/// <summary>Brankář. Rating 45–95 určuje, kolik toho chytí.</summary>
public sealed class Goalkeeper
{
    public string Name { get; }
    public int Rating { get; }

    public Goalkeeper(string name, int rating)
    {
        Name = name;
        Rating = rating;
    }

    public string Tier => Rating switch
    {
        >= 88 => "Světová třída",
        >= 82 => "Výborný",
        >= 74 => "Solidní",
        >= 62 => "Průměrný",
        _ => "Slabý",
    };
}

/// <summary>Tým = barvy dresů, exekutoři a brankáři.</summary>
public sealed class Team
{
    public string Name { get; init; }
    public string Code { get; init; }
    public Color Shirt { get; init; }
    public Color Shorts { get; init; }
    public Color Socks { get; init; }
    public Color KeeperShirt { get; init; }
    /// <summary>Barvy vlajky (pro kartičku týmu v menu).</summary>
    public Color[] Flag { get; init; }
    public FlagStyle FlagStyle { get; init; } = FlagStyle.Horizontal;
    public List<Kicker> Kickers { get; init; } = new();
    public List<Goalkeeper> Keepers { get; init; } = new();

    public Goalkeeper BestKeeper => Keepers.OrderByDescending(k => k.Rating).First();

    public int AttackRating =>
        (int)Math.Round(Kickers.OrderByDescending(k => k.PenaltySkill).Take(5).Average(k => k.PenaltySkill));

    /// <summary>Pořadí exekutorů soupeře (AI posílá nejlepší střelce jako první).</summary>
    public IReadOnlyList<Kicker> PenaltyOrder =>
        Kickers.OrderByDescending(k => k.PenaltySkill).ToList();
}
