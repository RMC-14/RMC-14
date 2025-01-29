using Content.Client.Message;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Construction;

[UsedImplicitly]
public sealed class RMCConstructionBui : BoundUserInterface
{
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    [ViewVariables]
    private RMCConstructionWindow? _window;

    public RMCConstructionBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        _window = this.CreateWindow<RMCConstructionWindow>();

        Refresh();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        Refresh();
    }

    public void Refresh()
    {
        if (_window == null)
            return;

        if (!EntMan.TryGetComponent(Owner, out XenoEvolutionComponent? xeno))
            return;

        _window.PointsLabel.Visible = xeno.Max > FixedPoint2.Zero;

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

        _window.Separator.Visible = _window.EvolutionsContainer.ChildCount > 0 && _window.StrainsContainer.ChildCount > 0;

        var lackingOvipositor = State is XenoEvolveBuiState { LackingOvipositor: true };
        var points = xeno.Points;
        _window.PointsLabel.Text = $"Evolution points: {(int) Math.Floor(points.Double())} / {xeno.Max}";
        if (lackingOvipositor)
        {
            // TODO RMC14 for some reason this doesn't properly wrap text
            _window.OvipositorNeededLabel.SetMarkupPermissive("[bold][color=red]The Queen must be in their\novipositor for you to gain points![/color][/bold]");
            _window.OvipositorNeededLabel.Visible = xeno.Max > FixedPoint2.Zero;
        }
        else
        {
            _window.OvipositorNeededLabel.Visible = false;
        }
    }
}
