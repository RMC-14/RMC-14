using Content.Shared.Actions;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.GasToggle;

public sealed class XenoGasToggleSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    [Dependency] private readonly SharedActionsSystem _actions = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<XenoGasToggleComponent, XenoGasToggleActionEvent>(OnToggleType);
    }

    private void OnToggleType(Entity<XenoGasToggleComponent> xeno, ref XenoGasToggleActionEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;
        xeno.Comp.IsNeurotoxin = !xeno.Comp.IsNeurotoxin;
        _actions.SetToggled(args.Action, xeno.Comp.IsNeurotoxin);
        Dirty(xeno);
    }
}
