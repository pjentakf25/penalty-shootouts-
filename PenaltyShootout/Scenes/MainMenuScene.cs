using Microsoft.Xna.Framework;
using PenaltyShootout.Core;
using PenaltyShootout.UI;

namespace PenaltyShootout.Scenes;

public sealed class MainMenuScene : Scene
{
    private static readonly string[] Items = { "Hrát zápas", "Jak hrát", "Konec" };

    private readonly SelectionGrid _menu;
    private bool _help;

    public MainMenuScene(PenaltyGame game) : base(game)
    {
        _menu = new SelectionGrid
        {
            Count = Items.Length,
            RectFor = ItemRect,
            OnMove = game.Audio.Click,
        };
    }

    private static Rectangle ItemRect(int i) => new(Ui.Width / 2 - 170, 330 + i * 78, 340, 60);

    public override void Update(float dt)
    {
        Game.OrbitCamera();

        if (_help)
        {
            if (Input.Back || Input.Action) _help = false;
            return;
        }

        if (Input.Back) { Game.Exit(); return; }

        if (_menu.Update())
        {
            Game.Audio.Click();
            switch (_menu.Selected)
            {
                case 0: Game.Scenes.Change(new SetupScene(Game)); break;
                case 1: _help = true; break;
                case 2: Game.Exit(); break;
            }
        }
    }

    public override void Draw(float dt)
    {
        Game.DrawWorld();
        var sb = Game.SpriteBatch;
        Ui.Begin(sb);
        Ui.Fade(sb, Color.Black * 0.35f);

        Ui.Text(sb, "PENALTOVÝ", new Vector2(Ui.Width / 2f, 90), Color.White, 1.25f, Align.Center, title: true);
        Ui.Text(sb, "ROZSTŘEL 3D", new Vector2(Ui.Width / 2f, 170), Ui.Accent, 1.25f, Align.Center, title: true);
        Ui.Text(sb, "5 penalt, 5 různých exekutorů, skuteční hráči a brankáři", new Vector2(Ui.Width / 2f, 268),
            Color.White * 0.9f, 0.85f, Align.Center);

        for (int i = 0; i < Items.Length; i++)
            Widgets.Button(sb, ItemRect(i), Items[i], i == _menu.Selected);

        Ui.Text(sb, "Šipky / myš - výběr    Enter - potvrdit    F11 - celá obrazovka", new Vector2(Ui.Width / 2f, Ui.Height - 40),
            Color.White * 0.7f, 0.7f, Align.Center);

        if (_help) DrawHelp(sb);
        sb.End();
    }

    private static void DrawHelp(Microsoft.Xna.Framework.Graphics.SpriteBatch sb)
    {
        Ui.Fade(sb, Color.Black * 0.55f);
        var r = new Rectangle(Ui.Width / 2 - 430, 90, 860, 540);
        Ui.Panel(sb, r);
        Ui.Text(sb, "JAK HRÁT", new Vector2(r.Center.X, r.Y + 18), Ui.Accent, 0.7f, Align.Center, title: true);

        string[] lines =
        {
            "1. Vyber svůj tým, soupeře, jeho brankáře a prostředí.",
            "2. Před každou penaltou vyber exekutora - každý hráč kope jen jednou.",
            "3. Střela má 3 kroky - pokaždé zmáčkni MEZERNÍK nebo klikni:",
            "     a) směr do strany   b) výška   c) síla",
            "4. Zelená zóna síly = ideál. Červená = míč letí vysoko (přes břevno).",
            "5. Útočníci mají lepší Razanci i Umístění - ukazatele jsou pomalejší",
            "    a míč letí přesněji. Obránci se trefují hůř.",
            "6. Když kope soupeř, chytáš ty: A / D / W (nebo šipky) = skok",
            "    vlevo / vpravo / na střed. Když nic nezmáčkneš, chytá brankář sám.",
            "7. Top brankáři chytí skoro všechno, slabí skoro nic. Míř k tyčím!",
            "8. Tip: hodně slabá střela doprostřed nahoru = Panenka.",
        };
        for (int i = 0; i < lines.Length; i++)
            Ui.Text(sb, lines[i], new Vector2(r.X + 40, r.Y + 90 + i * 36), Color.White, 0.85f);

        Ui.Text(sb, "Enter / Esc - zpět", new Vector2(r.Center.X, r.Bottom - 40), Ui.Muted, 0.75f, Align.Center);
    }
}
