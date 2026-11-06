using System.Collections.Generic;
using Microsoft.Xna.Framework;
using static PenaltyShootout.Data.PlayerPosition;

namespace PenaltyShootout.Data;

/// <summary>
/// Reálné týmy, hráči a brankáři. Hodnoty Razance/Umístění a rating brankářů
/// jsou herní odhady (útočníci kopou penalty lépe než obránci).
/// </summary>
public static class TeamDatabase
{
    public static IReadOnlyList<Team> Teams { get; } = new List<Team>
    {
        new()
        {
            Name = "Česko", Code = "CZE",
            Shirt = new Color(205, 25, 35), Shorts = Color.White, Socks = new Color(205, 25, 35),
            KeeperShirt = new Color(240, 210, 30),
            Flag = new[] { Color.White, new Color(215, 20, 26), new Color(17, 69, 126) }, FlagStyle = FlagStyle.Czech,
            Keepers = { new("Matěj Kovář", 80), new("Jindřich Staněk", 77), new("Antonín Kinský", 72) },
            Kickers =
            {
                new("Patrik Schick", Forward, 88, 86),
                new("Adam Hložek", Forward, 80, 78),
                new("Tomáš Chorý", Forward, 87, 72),
                new("Lukáš Provod", Midfielder, 80, 79),
                new("Pavel Šulc", Midfielder, 76, 81),
                new("Tomáš Souček", Midfielder, 83, 75),
                new("Vladimír Coufal", Defender, 71, 63),
                new("Ladislav Krejčí", Defender, 74, 61),
            },
        },
        new()
        {
            Name = "Španělsko", Code = "ESP",
            Shirt = new Color(190, 18, 32), Shorts = new Color(22, 32, 82), Socks = new Color(22, 32, 82),
            KeeperShirt = new Color(40, 180, 90),
            Flag = new[] { new Color(170, 21, 27), new Color(241, 191, 0), new Color(170, 21, 27) },
            Keepers = { new("Unai Simón", 86), new("David Raya", 85), new("Álex Remiro", 80) },
            Kickers =
            {
                new("Lamine Yamal", Forward, 80, 89),
                new("Mikel Oyarzabal", Forward, 82, 88),
                new("Álvaro Morata", Forward, 85, 82),
                new("Nico Williams", Forward, 83, 79),
                new("Pedri", Midfielder, 74, 86),
                new("Rodri", Midfielder, 85, 85),
                new("Dani Carvajal", Defender, 72, 67),
                new("Aymeric Laporte", Defender, 75, 62),
            },
        },
        new()
        {
            Name = "Francie", Code = "FRA",
            Shirt = new Color(28, 42, 102), Shorts = Color.White, Socks = new Color(200, 30, 45),
            KeeperShirt = new Color(60, 60, 66),
            Flag = new[] { new Color(0, 35, 149), Color.White, new Color(237, 41, 57) }, FlagStyle = FlagStyle.Vertical,
            Keepers = { new("Mike Maignan", 89), new("Brice Samba", 79), new("Lucas Chevalier", 78) },
            Kickers =
            {
                new("Kylian Mbappé", Forward, 93, 91),
                new("Antoine Griezmann", Forward, 82, 92),
                new("Ousmane Dembélé", Forward, 86, 82),
                new("Marcus Thuram", Forward, 85, 79),
                new("Aurélien Tchouaméni", Midfielder, 84, 77),
                new("Adrien Rabiot", Midfielder, 80, 74),
                new("Theo Hernández", Defender, 87, 70),
                new("William Saliba", Defender, 72, 58),
            },
        },
        new()
        {
            Name = "Anglie", Code = "ENG",
            Shirt = new Color(245, 245, 245), Shorts = new Color(20, 30, 75), Socks = new Color(245, 245, 245),
            KeeperShirt = new Color(235, 110, 30),
            Flag = new[] { Color.White, new Color(206, 17, 36) }, FlagStyle = FlagStyle.Cross,
            Keepers = { new("Jordan Pickford", 86), new("Aaron Ramsdale", 79), new("Dean Henderson", 78) },
            Kickers =
            {
                new("Harry Kane", Forward, 94, 93),
                new("Cole Palmer", Midfielder, 83, 92),
                new("Bukayo Saka", Forward, 82, 86),
                new("Jude Bellingham", Midfielder, 85, 84),
                new("Phil Foden", Midfielder, 80, 85),
                new("Declan Rice", Midfielder, 83, 75),
                new("John Stones", Defender, 68, 64),
                new("Harry Maguire", Defender, 76, 58),
            },
        },
        new()
        {
            Name = "Argentina", Code = "ARG",
            Shirt = new Color(120, 185, 235), Shorts = new Color(25, 25, 30), Socks = new Color(25, 25, 30),
            KeeperShirt = new Color(150, 60, 200),
            Flag = new[] { new Color(116, 172, 223), Color.White, new Color(116, 172, 223) },
            Keepers = { new("Emiliano Martínez", 91), new("Gerónimo Rulli", 80), new("Walter Benítez", 77) },
            Kickers =
            {
                new("Lionel Messi", Forward, 84, 95),
                new("Lautaro Martínez", Forward, 88, 86),
                new("Julián Álvarez", Forward, 86, 88),
                new("Alejandro Garnacho", Forward, 84, 76),
                new("Enzo Fernández", Midfielder, 80, 80),
                new("Alexis Mac Allister", Midfielder, 78, 86),
                new("Rodrigo De Paul", Midfielder, 81, 76),
                new("Cristian Romero", Defender, 77, 60),
            },
        },
        new()
        {
            Name = "Portugalsko", Code = "POR",
            Shirt = new Color(155, 18, 38), Shorts = new Color(20, 95, 55), Socks = new Color(155, 18, 38),
            KeeperShirt = new Color(250, 200, 0),
            Flag = new[] { new Color(0, 102, 0), new Color(0, 102, 0), new Color(255, 0, 0), new Color(255, 0, 0), new Color(255, 0, 0) }, FlagStyle = FlagStyle.Vertical,
            Keepers = { new("Diogo Costa", 89), new("José Sá", 80), new("Rui Patrício", 79) },
            Kickers =
            {
                new("Cristiano Ronaldo", Forward, 95, 92),
                new("Bruno Fernandes", Midfielder, 86, 93),
                new("Bernardo Silva", Midfielder, 72, 86),
                new("Rafael Leão", Forward, 87, 76),
                new("João Félix", Forward, 80, 83),
                new("Vitinha", Midfielder, 76, 82),
                new("Rúben Dias", Defender, 75, 60),
                new("Nuno Mendes", Defender, 74, 65),
            },
        },
        new()
        {
            Name = "Německo", Code = "GER",
            Shirt = new Color(245, 245, 245), Shorts = new Color(20, 20, 22), Socks = new Color(245, 245, 245),
            KeeperShirt = new Color(220, 40, 140),
            Flag = new[] { new Color(20, 20, 20), new Color(221, 0, 0), new Color(255, 206, 0) },
            Keepers = { new("Marc-André ter Stegen", 88), new("Oliver Baumann", 79), new("Alexander Nübel", 78) },
            Kickers =
            {
                new("Kai Havertz", Forward, 86, 87),
                new("Florian Wirtz", Midfielder, 82, 88),
                new("Jamal Musiala", Midfielder, 78, 86),
                new("Joshua Kimmich", Midfielder, 80, 88),
                new("İlkay Gündoğan", Midfielder, 78, 88),
                new("Niclas Füllkrug", Forward, 90, 80),
                new("Antonio Rüdiger", Defender, 89, 62),
                new("Jonathan Tah", Defender, 73, 60),
            },
        },
        new()
        {
            Name = "Brazílie", Code = "BRA",
            Shirt = new Color(250, 215, 25), Shorts = new Color(20, 70, 170), Socks = Color.White,
            KeeperShirt = new Color(230, 60, 60),
            Flag = new[] { new Color(0, 155, 58), new Color(254, 223, 0), new Color(0, 39, 118) }, FlagStyle = FlagStyle.Brazil,
            Keepers = { new("Alisson Becker", 90), new("Ederson", 86), new("Bento", 78) },
            Kickers =
            {
                new("Vinícius Júnior", Forward, 86, 80),
                new("Raphinha", Forward, 87, 84),
                new("Rodrygo", Forward, 80, 84),
                new("Neymar", Forward, 82, 93),
                new("Endrick", Forward, 86, 75),
                new("Casemiro", Midfielder, 85, 77),
                new("Marquinhos", Defender, 72, 66),
                new("Gabriel Magalhães", Defender, 81, 58),
            },
        },
        new()
        {
            Name = "Gibraltar", Code = "GIB",
            Shirt = new Color(240, 240, 240), Shorts = new Color(200, 20, 40), Socks = new Color(200, 20, 40),
            KeeperShirt = new Color(45, 45, 50),
            Flag = new[] { Color.White, Color.White, new Color(218, 0, 12) },
            Keepers = { new("Dayle Coleing", 58), new("Jaylan Hankins", 52), new("Bradley Banda", 47) },
            Kickers =
            {
                new("Liam Walker", Midfielder, 66, 68),
                new("Tjay De Barr", Forward, 64, 60),
                new("Lee Casciaro", Forward, 60, 62),
                new("Liam Jessop", Forward, 58, 54),
                new("Ayoub El Hmidi", Midfielder, 57, 55),
                new("Graeme Torrilla", Midfielder, 56, 57),
                new("Roy Chipolina", Defender, 58, 52),
                new("Joseph Chipolina", Defender, 55, 50),
            },
        },
    };
}
