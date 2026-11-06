# Penaltový rozstřel 3D

3D penaltová hra v **MonoGame** (DesktopGL, .NET 9) ve stylu mobilních fotbálků
(low-poly kostičkoví hráči, kamera za exekutorem). Veškerá grafika i zvuky se
generují v kódu – jediné assety jsou dva fonty.

## Spuštění

```bash
dotnet run --project PenaltyShootout
```

Nebo otevři `penalty-shootout.slnx` ve Visual Studiu a spusť projekt `PenaltyShootout`.

## Jak se hraje

1. **Tým → soupeř → brankář soupeře → prostředí** (stadion den/noc, venkovní hřiště, zimní hřiště).
2. Před každou sérií vybereš **exekutora** – každý hráč kope jen jednou.
3. Střela ve 3 krocích, vždy **mezerník / klik** ve správnou chvíli:
   **směr** do strany → **výška** → **síla** (zelená zóna = ideál, červená = přes břevno).
4. Když kope soupeř, **chytáš ty**: `A` / `W` / `D` (nebo šipky) = skok vlevo / střed / vpravo.
5. 5 sérií, při remíze **náhlá smrt**. Rozstřel končí, jakmile je matematicky rozhodnuto.

| Klávesa | Akce |
|---|---|
| Mezerník / Enter / levé tlačítko | potvrdit, zastavit ukazatel |
| Šipky / WASD / myš | výběr v menu, skok brankáře |
| Esc | zpět / pauza |
| F11 | celá obrazovka |

**Hráči:** útočníci mají vyšší *Razanci* (rychlost míče) i *Umístění* (přesnost a pomalejší ukazatele),
obránci se trefují hůř. **Brankáři:** rating 47–91 – top brankář chytí většinu, slabý skoro nic.
Tip: hodně slabá střela doprostřed nahoru = **Panenka**.

## Struktura projektu

```
PenaltyShootout/
├─ Program.cs, PenaltyGame.cs   vstupní bod, sdílené služby, vykreslení světa
├─ Core/       scény, vstup, náhoda, procedurální zvuky, ladicí screenshoty
├─ Data/       hráči, brankáři, týmy (TeamDatabase), prostředí, stav zápasu
├─ Gameplay/   pravidla střely a zákroku (ShotResolver), animace střelce a brankáře
├─ Rendering/  vrcholy, sítě, MeshBuilder (kvádry/válce/koule), Renderer3D + kamera
├─ World/      hřiště a branky, stadion/park, postavičky, míč, sníh
├─ Scenes/     Menu → Setup → KickerSelect → Penalty → Result
├─ UI/         kreslení 2D (Ui), výběrová mřížka, widgety (skóre, karty, vlajky)
└─ Content/    fonty (Arial, Arial Black)
```

Úprava týmů a hodnot hráčů: `PenaltyShootout/Data/TeamDatabase.cs`.
Vyvážení obtížnosti: `PenaltyShootout/Gameplay/ShotResolver.cs` (`SaveChance`, `Spread`, rychlosti ukazatelů).

Ladění: `PenaltyShootout.exe --screenshots <složka>` projde hlavní obrazovky a uloží PNG snímky.
