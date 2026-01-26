using Content.Shared._RMC14.Targeting;
using Content.Shared._RMC14.Targeting.Focused;

namespace Content.Shared._RMC14.Weapons.Ranged.AimedShot.FocusedShooting;

public sealed class RMCFocusedShootingSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCFocusedShootingComponent, AimedShotEvent>(OnAimedShot);
        SubscribeLocalEvent<RMCFocusedShootingComponent, TargetingStartedEvent>(OnTargetingStarted);
    }

    /// <summary>
    ///     Decide the color of the laser and type of targeting effect to apply on the target based on the amount of focus stacks.
    /// </summary>
    private void OnTargetingStarted(Entity<RMCFocusedShootingComponent> ent, ref TargetingStartedEvent args)
    {
        var focusCounter = ent.Comp.FocusCounter;

        if (args.Target != ent.Comp.CurrentTarget || ent.Comp.FocusCounter > 2)
            focusCounter = 0;

        if (TryComp(ent, out TargetingLaserComponent? targetingLaser))
        {
            if (focusCounter == 2)
                targetingLaser.CurrentLaserColor = ent.Comp.LaserColor;
            else
                targetingLaser.CurrentLaserColor = targetingLaser.LaserColor;

            Dirty(ent, targetingLaser);
        }

        if (focusCounter < 2 || args.TargetedEffect != TargetedEffects.Targeted)
            return;

        args.TargetedEffect = TargetedEffects.TargetedIntense;
    }
    /// <summary>
    ///     Change the focus counter when an aimed shot is performed.
    /// </summary>
    private void OnAimedShot(Entity<RMCFocusedShootingComponent> ent, ref AimedShotEvent args)
    {
        var focusCounter = ent.Comp.FocusCounter;
        var currentTarget = ent.Comp.CurrentTarget;
        var user = _transform.GetParentUid(ent);
        if (currentTarget != args.Target && TryComp(currentTarget, out RMCBeingFocusedComponent? beingFocused))
        {
            beingFocused.FocusedBy.Remove(user);
            Dirty(currentTarget.Value, beingFocused);

            if (beingFocused.FocusedBy.Count == 0)
                RemComp<RMCBeingFocusedComponent>(currentTarget.Value);
        }

        var focused = EnsureComp<RMCBeingFocusedComponent>(args.Target);
        focused.FocusedBy.Add(user);
        Dirty(args.Target, focused);

        if (currentTarget == args.Target)
        {
            if (ent.Comp.FocusCounter > 2)
                focusCounter = 0;
        }
        else
        {
            ent.Comp.CurrentTarget = args.Target;
            focusCounter = 0;
        }

        ent.Comp.FocusCounter = Math.Min(focusCounter + 1, 3);
        Dirty(ent);
    }
}
