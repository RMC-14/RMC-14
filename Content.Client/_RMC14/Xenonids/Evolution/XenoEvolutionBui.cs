using Content.Client._RMC14.Xenonids.UI;
using Content.Client.Message;
using Content.Shared._RMC14.Xenonids.Evolution;
using Content.Shared._RMC14.Xenonids.Strain;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Xenonids.Evolution;

[UsedImplicitly]
public sealed class XenoEvolutionBui : BoundUserInterface
{
    [Dependency] private readonly IComponentFactory _compFactory = default!;
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
        base.Open();
        _window = this.CreateWindow<XenoEvolutionWindow>();

        if (EntMan.TryGetComponent(Owner, out XenoEvolutionComponent? xeno))
        {
            foreach (var strain in xeno.Strains)
            {
                AddStrain(strain);
            }
        }

        _window.StrainsLabel.Visible = _window.StrainsContainer.ChildCount > 0;

        Refresh();
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

    private void AddStrain(EntProtoId strainId)
    {
        if (_window is not { IsOpen: true })
            return;

        if (!_prototype.TryIndex(strainId, out var strain))
            return;

        var control = new XenoChoiceControl();
        var name = strain.Name;
        if (strain.TryGetComponent(out XenoStrainComponent? strainComp, _compFactory))
        {
            name = $"{Loc.GetString(strainComp.Name)} {name}";

            if (strainComp.Description is { } description)
            {
                control.Button.ToolTip = Loc.GetString(description);
                control.Button.TooltipDelay = 0.1f;
            }
        }

        control.Set(name, _sprite.Frame0(strain));

        control.Button.OnPressed += _ =>
        {
            SendPredictedMessage(new XenoStrainBuiMsg(strainId));
            Close();
        };

        _window.StrainsContainer.AddChild(control);
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
