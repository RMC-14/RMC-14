using Content.Shared.Actions.Events;

namespace Content.Shared._RMC14.Xenonids.Actions;

public sealed class XenoActionsSystem : EntitySystem
{
    [Dependency] private readonly XenoSystem _xeno = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoOffensiveActionComponent, ActionValidateEvent>(OnValidateActionEntityTarget);
    }

    private void OnValidateActionEntityTarget(Entity<XenoOffensiveActionComponent> ent, ref ActionValidateEvent args)
    {
        if (args.Invalid)
            return;

        if (GetEntity(args.Input.EntityTarget) is not { } target)
            return;

        if (!_xeno.CanAbilityAttackTarget(args.User, target, false, true))
            args.Invalid = true;
    }
}
