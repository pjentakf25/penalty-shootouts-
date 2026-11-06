using Microsoft.Xna.Framework;

namespace PenaltyShootout.Data;

/// <summary>Prostředí, ve kterém se rozstřel hraje.</summary>
public enum Arena { StadiumDay, StadiumNight, Park, Winter }

/// <summary>Vzhled prostředí – barvy oblohy, světla, mlhy a trávy.</summary>
public sealed class ArenaStyle
{
    public Arena Arena { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }

    public Color SkyTop { get; init; }
    public Color SkyBottom { get; init; }
    public Vector3 Ambient { get; init; }
    public Vector3 LightDirection { get; init; }
    public Vector3 LightColor { get; init; }
    public Vector3 FillColor { get; init; }
    public Color FogColor { get; init; }
    public float FogStart { get; init; }
    public float FogEnd { get; init; }

    public Color Grass1 { get; init; }
    public Color Grass2 { get; init; }
    public Color Surround { get; init; }

    public bool HasStands { get; init; }
    public bool Night { get; init; }
    public bool Snow { get; init; }
    public bool Clouds { get; init; }
    public float CrowdVolume { get; init; }

    public static readonly Arena[] All = { Arena.StadiumDay, Arena.StadiumNight, Arena.Park, Arena.Winter };

    public static ArenaStyle For(Arena a) => a switch
    {
        Arena.StadiumNight => new ArenaStyle
        {
            Arena = a, Name = "Stadion - noc",
            Description = "Vyprodaný stadion pod reflektory.",
            SkyTop = new Color(4, 6, 18), SkyBottom = new Color(22, 30, 62),
            Ambient = new Vector3(0.36f, 0.37f, 0.45f),
            LightDirection = Vector3.Normalize(new Vector3(0.25f, -1f, 0.45f)),
            LightColor = new Vector3(0.85f, 0.86f, 0.95f),
            FillColor = new Vector3(0.25f, 0.25f, 0.32f),
            FogColor = new Color(18, 24, 48), FogStart = 70, FogEnd = 260,
            Grass1 = new Color(46, 128, 52), Grass2 = new Color(38, 112, 44),
            Surround = new Color(48, 50, 56),
            HasStands = true, Night = true, CrowdVolume = 0.35f,
        },
        Arena.Park => new ArenaStyle
        {
            Arena = a, Name = "Venkovní hřiště",
            Description = "Okresní přebor – stromy, plot a klubovna.",
            SkyTop = new Color(70, 140, 230), SkyBottom = new Color(190, 222, 246),
            Ambient = new Vector3(0.50f, 0.50f, 0.52f),
            LightDirection = Vector3.Normalize(new Vector3(-0.5f, -0.9f, -0.35f)),
            LightColor = new Vector3(0.95f, 0.90f, 0.78f),
            FillColor = new Vector3(0.22f, 0.25f, 0.32f),
            FogColor = new Color(190, 222, 246), FogStart = 120, FogEnd = 420,
            Grass1 = new Color(78, 150, 58), Grass2 = new Color(68, 138, 50),
            Surround = new Color(70, 128, 48),
            Clouds = true, CrowdVolume = 0.06f,
        },
        Arena.Winter => new ArenaStyle
        {
            Arena = a, Name = "Zimní hřiště",
            Description = "Mráz, sníh a sněhulák u rohového praporku.",
            SkyTop = new Color(150, 160, 178), SkyBottom = new Color(214, 220, 230),
            Ambient = new Vector3(0.60f, 0.62f, 0.66f),
            LightDirection = Vector3.Normalize(new Vector3(-0.3f, -1f, -0.2f)),
            LightColor = new Vector3(0.70f, 0.74f, 0.82f),
            FillColor = new Vector3(0.25f, 0.27f, 0.32f),
            FogColor = new Color(214, 220, 230), FogStart = 35, FogEnd = 170,
            Grass1 = new Color(112, 150, 104), Grass2 = new Color(100, 140, 94),
            Surround = new Color(236, 240, 246),
            Snow = true, CrowdVolume = 0.05f,
        },
        _ => new ArenaStyle
        {
            Arena = Arena.StadiumDay, Name = "Stadion - den",
            Description = "Slunečné odpoledne, plné tribuny.",
            SkyTop = new Color(55, 125, 220), SkyBottom = new Color(172, 212, 246),
            Ambient = new Vector3(0.46f, 0.46f, 0.50f),
            LightDirection = Vector3.Normalize(new Vector3(-0.45f, -1f, -0.3f)),
            LightColor = new Vector3(0.92f, 0.89f, 0.80f),
            FillColor = new Vector3(0.22f, 0.24f, 0.30f),
            FogColor = new Color(172, 212, 246), FogStart = 150, FogEnd = 480,
            Grass1 = new Color(62, 150, 60), Grass2 = new Color(52, 136, 52),
            Surround = new Color(64, 66, 72),
            HasStands = true, Clouds = true, CrowdVolume = 0.3f,
        },
    };
}
