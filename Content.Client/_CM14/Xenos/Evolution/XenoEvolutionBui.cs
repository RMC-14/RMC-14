using Content.Client._CM14.Xenos.UI;
using Content.Shared._CM14.Xenos.Evolution;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;
using XenoComponent = Content.Shared._CM14.Xenos.XenoComponent;

namespace Content.Client._CM14.Xenos.Evolution;

[UsedImplicitly]
public sealed class XenoEvolutionBui : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly SpriteSystem _sprite;

    [ViewVariables]
    private XenoEvolutionWindow? _window;

    public XenoEvolutionBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _sprite = EntMan.System<SpriteSystem>();
    }

    protected override void Open()
    {
        _window = new XenoEvolutionWindow();
        _window.OnClose += Close;

        if (EntMan.TryGetComponent(Owner, out XenoComponent? xeno))
        {
            foreach (var evolutionId in xeno.EvolvesTo)
            {
                if (!_prototype.TryIndex(evolutionId, out var evolution))
                    continue;

                var control = new XenoChoiceControl();
                control.Set(evolution.Name, _sprite.Frame0(evolution));

                var id = evolutionId;
                control.Button.OnPressed += _ =>
                {
                    SendMessage(new XenoEvolveBuiMessage(id));
                    Close();
                };

                _window.EvolutionsContainer.AddChild(control);
            }
        }

        _window.OpenCentered();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _window?.Dispose();
    }
}
