namespace PenaltyShootout.Core;

/// <summary>Základ pro všechny obrazovky hry (menu, výběr, samotná penalta...).</summary>
public abstract class Scene
{
    protected PenaltyGame Game { get; }

    protected Scene(PenaltyGame game) => Game = game;

    public virtual void Enter() { }
    public virtual void Leave() { }
    public abstract void Update(float dt);
    public abstract void Draw(float dt);
}

/// <summary>Přepínání scén. Změna se provede až na začátku dalšího Update.</summary>
public sealed class SceneManager
{
    private Scene _current;
    private Scene _pending;

    public Scene Current => _current;

    public void Change(Scene scene) => _pending = scene;

    public void Update(float dt)
    {
        if (_pending != null)
        {
            _current?.Leave();
            _current = _pending;
            _pending = null;
            _current.Enter();
        }
        _current?.Update(dt);
    }

    public void Draw(float dt) => _current?.Draw(dt);
}
