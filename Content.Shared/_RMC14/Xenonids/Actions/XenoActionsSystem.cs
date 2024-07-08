using Content.Shared.Actions.Events;

namespace Content.Shared._RMC14.Xenonids.Actions;

public sealed class XenoActionsSystem : EntitySystem
{
    [Dependency] private readonly XenoSystem _xeno = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoOffensiveActionComponent, ValidateActionEntityTargetEvent>(OnValidateActionEntityTarget);
    }

    private void OnValidateActionEntityTarget(Entity<XenoOffensiveActionComponent> ent, ref ValidateActionEntityTargetEvent args)
    {
        if (args.Cancelled)
            return;

        if (!_xeno.CanAbilityAttackTarget(args.User, args.Target))
            args.Cancelled = true;
    }
}
