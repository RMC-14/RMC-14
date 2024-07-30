using Content.Client._RMC14.Xenonids.UI;
using Content.Client.Message;
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
    private readonly XenoEvolutionSystem _xenoEvolution;

    [ViewVariables]
    private XenoEvolutionWindow? _window;

    public XenoEvolutionBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _sprite = EntMan.System<SpriteSystem>();
        _xenoEvolution = EntMan.System<XenoEvolutionSystem>();
    }

    protected override void Open()
    {
        _window = new XenoEvolutionWindow();
        _window.OnClose += Close;

        Refresh();

        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        Refresh();
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

    public void Refresh()
    {
        if (_window == null)
            return;

        if (!EntMan.TryGetComponent(Owner, out XenoEvolutionComponent? xeno))
            return;

        _window.EvolutionsContainer.DisposeAllChildren();
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

        var lackingOvipositor = State is XenoEvolveBuiState { LackingOvipositor: true };
        _window.PointsLabel.Text = $"Evolution points: {xeno.Points} / {xeno.Max}";
        if (lackingOvipositor)
        {
            // TODO RMC14 for some reason this doesn't properly wrap text
            _window.OvipositorNeededLabel.SetMarkupPermissive("[bold][color=red]The Queen must be in their\novipositor for you to gain points![/color][/bold]");
            _window.OvipositorNeededLabel.Visible = true;
        }
        else
        {
            _window.OvipositorNeededLabel.Visible = false;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _window?.Dispose();
    }
}
