using Content.Shared.CM14.Xenos;
using Content.Shared.CM14.Xenos.Evolution;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client._CM14.Xenos.Evolution;

[UsedImplicitly]
public sealed class XenoEvolutionBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly SpriteSystem _sprite;

    [ViewVariables]
    private XenoEvolutionWindow? _window;

    public XenoEvolutionBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _sprite = EntMan.System<SpriteSystem>();
    }

    protected override void Open()
    {
        base.Open();

        _window = new XenoEvolutionWindow();

        if (EntMan.TryGetComponent(Owner, out XenoComponent? xeno))
        {
            for (var i = 0; i < xeno.EvolvesTo.Count; i++)
            {
                var evolutionId = xeno.EvolvesTo[i];
                if (!_prototype.TryIndex(evolutionId, out var evolution))
                    continue;

                var control = new XenoEvolutionChoiceControl();
                control.Texture.Texture = _sprite.Frame0(evolution);
                control.NameLabel.SetMessage(evolution.Name);

                var index = i;
                control.Button.OnPressed += _ =>
                {
                    SendMessage(new EvolveBuiMessage(index));
                    Close();
                };

                _window.EvolutionsContainer.AddChild(control);
            }
        }

        _window.OpenCentered();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            _window?.Dispose();
    }
}
