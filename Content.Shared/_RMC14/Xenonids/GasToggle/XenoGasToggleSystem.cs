using Content.Shared.Actions;

namespace Content.Shared._RMC14.Xenonids.GasToggle;

public sealed class XenoGasToggleSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoGasToggleComponent, XenoGasToggleActionEvent>(OnToggleType);
    }

    private void OnToggleType(Entity<XenoGasToggleComponent> xeno, ref XenoGasToggleActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        xeno.Comp.IsNeurotoxin = !xeno.Comp.IsNeurotoxin;
        _actions.SetToggled(args.Action.AsNullable(), xeno.Comp.IsNeurotoxin);
        Dirty(xeno);
    }
}
