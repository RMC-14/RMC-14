using System.Linq;
using Content.Client._RMC14.Xenonids.UI;
using Content.Client.Message;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Evolution;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Strain;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
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
    private readonly MobStateSystem _mobState;

    [ViewVariables]
    private XenoEvolutionWindow? _window;

    private readonly Dictionary<EntProtoId, XenoChoiceControl> _evolutionControls = new();
    private readonly Dictionary<EntProtoId, XenoChoiceControl> _strainControls = new();

    private bool _phaseAActive;

    public XenoEvolutionBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _sprite = EntMan.System<SpriteSystem>();
        _mobState = EntMan.System<MobStateSystem>();
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<XenoEvolutionWindow>();
        _window.OvipositorNeededLabel.Visible = false;

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

        if (!_evolutionControls.TryGetValue(evolutionId, out var control))
        {
            control = new XenoChoiceControl();
            control.Set(evolution.Name, _sprite.Frame0(evolution));

            control.Button.OnPressed += _ =>
            {
                SendPredictedMessage(new XenoEvolveBuiMsg(evolutionId));
                Close();
            };

            _evolutionControls[evolutionId] = control;
            _window?.EvolutionsContainer.AddChild(control);
        }

        control.Visible = true;
        control.Button.Disabled = false;
    }

    private void AddRaffleChoice(EntProtoId targetId, int candidateCount, bool queued)
    {
        if (_window is not { IsOpen: true })
            return;

        if (!_prototype.TryIndex(targetId, out var target))
            return;

        var control = new XenoChoiceControl();

        control.Set($"{target.Name} ({candidateCount})", _sprite.Frame0(target));

        if (queued)
        {
            control.Button.OnPressed += _ => SendPredictedMessage(new XenoLeaveRaffleBuiMsg());
        }
        else
        {
            control.Button.OnPressed += _ => SendPredictedMessage(new XenoJoinRaffleBuiMsg(targetId));
        }

        control.Visible = true;
        control.Button.Disabled = false;
        _evolutionControls[targetId] = control;

        var container = _phaseAActive ? _window.RaffleContainer : _window.EvolutionsContainer;
        container.AddChild(control);
    }

    private void AddStrain(EntProtoId strainId)
    {
        if (_window is not { IsOpen: true })
            return;

        if (!_prototype.TryIndex(strainId, out var strain))
            return;

        if (!_strainControls.TryGetValue(strainId, out var control))
        {
            control = new XenoChoiceControl();

            var name = strain.Name;
            string? description = null;

            if (strain.TryGetComponent(out XenoStrainComponent? strainComp))
            {
                name = $"{Loc.GetString(strainComp.Name)} {name}";
                description = strainComp.Description;
            }

            control.Set(name, _sprite.Frame0(strain));
            control.Button.Disabled = false;

            control.Button.OnPressed += _ =>
            {
                var confirmWindow = new XenoStrainConfirmWindow();
                confirmWindow.SetInfo(name, _sprite.Frame0(strain), description);

                confirmWindow.OnConfirm += () =>
                {
                    SendPredictedMessage(new XenoStrainBuiMsg(strainId));
                    confirmWindow.Close();
                    Close();
                };

                confirmWindow.OpenCentered();
            };

            _strainControls[strainId] = control;
            _window.StrainsContainer.AddChild(control);
        }

        control.Visible = true;
        control.Button.Disabled = false;
    }

    public void Refresh()
    {
        if (_window == null)
            return;

        if (!EntMan.TryGetComponent(Owner, out XenoEvolutionComponent? xeno))
            return;

        _window.PointsLabel.Visible = xeno.Max > FixedPoint2.Zero;

        _window.EvolutionsContainer.RemoveAllChildren();
        _window.RaffleContainer.RemoveAllChildren();
        _evolutionControls.Clear();

        var state = State as XenoEvolveBuiState;
        _phaseAActive = state?.PhaseAActive ?? false;
        EntMan.TryGetComponent(Owner, out XenoRaffleCandidateComponent? myCandidate);

        var hasQueenAlive = HiveHasLivingQueen();
        foreach (var evolutionId in xeno.EvolvesToWithoutPoints)
        {
            if (hasQueenAlive &&
                _prototype.TryIndex(evolutionId, out var proto) &&
                proto.TryGetComponent(out XenoEvolutionGranterComponent? _, _compFactory))
            {
                continue;
            }

            AddEvolutionOrRaffle(evolutionId, state, myCandidate);
        }

        if (xeno.Points >= xeno.Max)
        {
            foreach (var evolutionId in xeno.EvolvesTo)
                AddEvolutionOrRaffle(evolutionId, state, myCandidate);

            if (!xeno.MarinesLanded)
            {
                foreach (var evolutionId in xeno.EarlyEvolvesTo)
                    AddEvolutionOrRaffle(evolutionId, state, myCandidate);
            }
        }

        if (state != null)
        {
            foreach (var targetId in state.LeapfrogTargets)
            {
                if (_evolutionControls.ContainsKey(targetId))
                    continue;

                var queued = myCandidate != null && myCandidate.Target == targetId;
                AddRaffleChoice(targetId, GetCandidateCount(state, targetId), queued);
            }
        }

        if (myCandidate != null &&
            !string.IsNullOrEmpty(myCandidate.Target.Id) &&
            !_evolutionControls.ContainsKey(myCandidate.Target))
        {
            AddRaffleChoice(myCandidate.Target, GetCandidateCount(state, myCandidate.Target), true);
        }

        _window.TabContainer.SetTabVisible(1, _phaseAActive);
        if (!_phaseAActive && _window.TabContainer.CurrentTab == 1)
            _window.TabContainer.CurrentTab = 0;

        _window.Separator.Visible = _window.EvolutionsContainer.Children.Any(child => child.Visible) &&
                                    _window.StrainsContainer.Children.Any(child => child.Visible);

        var lackingOvipositor = state is { LackingOvipositor: true };
        var points = xeno.Points;

        _window.PointsLabel.Text = Loc.GetString("rmc-xeno-ui-evolution-points",
            ("points", (int)Math.Floor(points.Double())),
            ("maxPoints", xeno.Max));

        if (lackingOvipositor && xeno.Max > FixedPoint2.Zero)
        {
            if (!_window.OvipositorNeededLabel.Visible)
            {
                _window.OvipositorNeededLabel.SetMarkupPermissive(Loc.GetString("rmc-xeno-ui-ovi-needed-label"));
                _window.OvipositorNeededLabel.Visible = true;
            }
        }
        else if (_window.OvipositorNeededLabel.Visible)
        {
            _window.OvipositorNeededLabel.Visible = false;
        }
    }

    private void AddEvolutionOrRaffle(EntProtoId evolutionId, XenoEvolveBuiState? state, XenoRaffleCandidateComponent? myCandidate)
    {
        var queued = myCandidate != null && myCandidate.Target == evolutionId;
        if (queued)
        {
            AddRaffleChoice(evolutionId, GetCandidateCount(state, evolutionId), true);
            return;
        }

        if (state != null && state.RaffleGatedTargets.Contains(evolutionId.Id))
        {
            AddRaffleChoice(evolutionId, GetCandidateCount(state, evolutionId), false);
            return;
        }

        AddEvolution(evolutionId);
    }

    private static int GetCandidateCount(XenoEvolveBuiState? state, EntProtoId targetId)
    {
        if (state == null || string.IsNullOrEmpty(targetId.Id))
            return 0;

        return state.RaffleCandidates.GetValueOrDefault(targetId.Id);
    }

    private bool HiveHasLivingQueen()
    {
        if (!EntMan.TryGetComponent(Owner, out HiveMemberComponent? member) ||
            member.Hive is not { } hive ||
            !EntMan.TryGetComponent(hive, out HiveComponent? hiveComp) ||
            hiveComp.CurrentQueen is not { } queen)
        {
            return false;
        }

        return !_mobState.IsDead(queen);
    }
}
