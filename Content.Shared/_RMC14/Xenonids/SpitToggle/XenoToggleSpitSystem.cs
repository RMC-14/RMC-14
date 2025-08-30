using Content.Shared._RMC14.Xenonids.Projectile.Spit.Standard;
using Content.Shared.Actions;

namespace Content.Shared._RMC14.Xenonids.SpitToggle;

public sealed class XenoToggleSpitSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<XenoToggleSpitComponent, XenoSpitToggleActionEvent>(OnToggleSpit);
    }

    private void OnToggleSpit(Entity<XenoToggleSpitComponent> xeno, ref XenoSpitToggleActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<XenoSpitComponent>(xeno, out var spit))
            return;

        args.Handled = true;

        xeno.Comp.UseAcid = !xeno.Comp.UseAcid;

        _actions.SetToggled(args.Action.AsNullable(), xeno.Comp.UseAcid);

        var proto = xeno.Comp.UseAcid ? xeno.Comp.AcidProto : xeno.Comp.NeuroProto;
        var cost = xeno.Comp.UseAcid ? xeno.Comp.AcidCost : xeno.Comp.NeuroCost;

        spit.PlasmaCost = cost;
        spit.ProjectileId = proto;
        Dirty(xeno);
    }
}
