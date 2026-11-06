using System.Collections.Generic;
using System.Linq;
using PenaltyShootout.Gameplay;

namespace PenaltyShootout.Data;

public sealed class KickRecord
{
    public Kicker Kicker { get; init; }
    public ShotOutcome Outcome { get; init; }
    public bool Scored => Outcome.IsGoal();
}

/// <summary>
/// Stav penaltového rozstřelu: 5 sérií, střídá se domácí (hráč) a soupeř (AI).
/// Při nerozhodném stavu následuje náhlá smrt.
/// </summary>
public sealed class MatchState
{
    public const int RegularRounds = 5;

    public Team Home { get; }
    public Team Away { get; }
    public Goalkeeper HomeKeeper { get; }
    public Goalkeeper AwayKeeper { get; }
    public Arena Arena { get; }

    public List<KickRecord> HomeKicks { get; } = new();
    public List<KickRecord> AwayKicks { get; } = new();

    public MatchState(Team home, Team away, Goalkeeper homeKeeper, Goalkeeper awayKeeper, Arena arena)
    {
        Home = home;
        Away = away;
        HomeKeeper = homeKeeper;
        AwayKeeper = awayKeeper;
        Arena = arena;
    }

    public int HomeGoals => HomeKicks.Count(k => k.Scored);
    public int AwayGoals => AwayKicks.Count(k => k.Scored);

    /// <summary>Číslo série, která se právě hraje (1, 2, ...).</summary>
    public int Round => HomeKicks.Count + 1;

    public bool SuddenDeath => HomeKicks.Count >= RegularRounds;

    /// <summary>Každý hráč kope jednou; když se vystřídají všichni, začíná se znovu.</summary>
    public bool IsUsed(Kicker k)
    {
        int n = Home.Kickers.Count;
        int cycleStart = HomeKicks.Count / n * n;
        return HomeKicks.Skip(cycleStart).Any(r => r.Kicker == k);
    }

    public KickRecord HomeRecordOf(Kicker k) => HomeKicks.LastOrDefault(r => r.Kicker == k);

    public Kicker NextAwayKicker()
    {
        var order = Away.PenaltyOrder;
        return order[AwayKicks.Count % order.Count];
    }

    public void Record(bool home, Kicker kicker, ShotOutcome outcome)
    {
        var rec = new KickRecord { Kicker = kicker, Outcome = outcome };
        (home ? HomeKicks : AwayKicks).Add(rec);
    }

    /// <summary>Je rozhodnuto? Pokud ano, vrátí vítěze.</summary>
    public bool IsFinished(out bool homeWins)
    {
        homeWins = false;
        int h = HomeGoals, a = AwayGoals;
        int hk = HomeKicks.Count, ak = AwayKicks.Count;

        if (hk <= RegularRounds && ak <= RegularRounds)
        {
            int hRemaining = RegularRounds - hk;
            int aRemaining = RegularRounds - ak;
            if (h > a + aRemaining) { homeWins = true; return true; }
            if (a > h + hRemaining) { homeWins = false; return true; }
            return false;
        }

        // náhlá smrt – rozhoduje se vždy po kompletní sérii
        if (hk == ak && h != a)
        {
            homeWins = h > a;
            return true;
        }
        return false;
    }
}
