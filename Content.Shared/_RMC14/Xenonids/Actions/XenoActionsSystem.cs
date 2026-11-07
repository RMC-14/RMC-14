using Content.Shared._RMC14.Actions;
using Content.Shared.Actions.Events;

namespace Content.Shared._RMC14.Xenonids.Actions;

public sealed class XenoActionsSystem : EntitySystem
{
    [Dependency] private readonly XenoSystem _xeno = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoOffensiveActionComponent, ActionValidateEvent>(OnValidateActionEntityTarget);
        SubscribeLocalEvent<XenoOffensiveActionComponent, CheckActionTargetEvent>(OnOffensiveActionCheckActionTarget);
    }

    private void OnValidateActionEntityTarget(Entity<XenoOffensiveActionComponent> ent, ref ActionValidateEvent args)
    {
        if (args.Invalid)
            return;

        if (GetEntity(args.Input.EntityTarget) is not { } target)
            return;

        if (!_xeno.CanAbilityAttackTarget(args.User, target, ent.Comp.CanHitBarricades, ent.Comp.CanHitWindows))
            args.Invalid = true;
    }

    private void OnOffensiveActionCheckActionTarget(Entity<XenoOffensiveActionComponent> ent, ref CheckActionTargetEvent args)
    {
        if (args.Skip)
            return;

        if (!_xeno.CanAbilityAttackTarget(args.User, args.Target, ent.Comp.CanHitBarricades, ent.Comp.CanHitWindows))
            args.Skip = true;
    }
}
