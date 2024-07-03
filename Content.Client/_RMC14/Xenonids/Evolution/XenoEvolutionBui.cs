using Content.Client._RMC14.Xenonids.UI;
using Content.Shared._RMC14.Xenonids.Evolution;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Xenonids.Evolution;

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

        if (EntMan.TryGetComponent(Owner, out XenoEvolutionComponent? xeno))
        {
            foreach (var evolutionId in xeno.EvolvesToWithoutPoints)
            {
                AddEvolution(evolutionId);
            }

            if (xeno.Points >= xeno.Max)
            {
                foreach (var evolutionId in xeno.EvolvesTo)
                {
                    AddEvolution(evolutionId);
                }
            }
        }

        _window.OpenCentered();
    }

    private void AddEvolution(EntProtoId evolutionId)
    {
        if (!_prototype.TryIndex(evolutionId, out var evolution))
            return;

        var control = new XenoChoiceControl();
        control.Set(evolution.Name, _sprite.Frame0(evolution));

        control.Button.OnPressed += _ =>
        {
            SendPredictedMessage(new XenoEvolveBuiMsg(evolutionId));
            Close();
        };

        _window?.EvolutionsContainer.AddChild(control);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _window?.Dispose();
    }
}
