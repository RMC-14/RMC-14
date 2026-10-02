using Content.Shared._RMC14.Marines.Squads;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Light.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Storage.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;

namespace Content.Shared._RMC14.Interaction;

public sealed class RMCInteractionSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<InteractedBlacklistComponent, GettingInteractedWithAttemptEvent>(OnBlacklistInteractionAttempt);
        SubscribeLocalEvent<InteractedBlacklistComponent, CombatModeShouldHandInteractEvent>(OnBlacklistCombatInteractionAttempt);
        SubscribeLocalEvent<NoHandsInteractionBlockedComponent, GettingInteractedWithAttemptEvent>(OnNoHandsInteractionAttempt);
        SubscribeLocalEvent<InsertBlacklistComponent, ContainerIsInsertingAttemptEvent>(OnInsertBlacklistContainerIsInsertingAttempt);
        SubscribeLocalEvent<IgnoreInteractionRangeComponent, InRangeOverrideEvent>(OnInRangeOverride);
    }

    private void OnNoHandsInteractionAttempt(Entity<NoHandsInteractionBlockedComponent> ent, ref GettingInteractedWithAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!HasComp<HandsComponent>(args.Uid))
            args.Cancelled = true;
    }

    private bool BlockInteraction(Entity<InteractedBlacklistComponent> ent, EntityUid user)
    {
        if (ent.Comp.Blacklist == null)
            return false;

        if (!TryComp(ent, out TransformComponent? xform))
            return false;

        if (ent.Comp.AnchoredOnly && !xform.Anchored)
            return false;

        // Allows xenos to disable flashlights, even though they normally can't interact with them.
        if (TryComp(ent, out HandheldLightComponent? handheldLight) && handheldLight.Activated)
            return false;

        if (_whitelist.IsValid(ent.Comp.Blacklist, user))
            return true;

        return false;
    }

    private void OnBlacklistInteractionAttempt(Entity<InteractedBlacklistComponent> ent, ref GettingInteractedWithAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        args.Cancelled = BlockInteraction(ent, args.Uid);
    }

    private void OnBlacklistCombatInteractionAttempt(Entity<InteractedBlacklistComponent> ent, ref CombatModeShouldHandInteractEvent args)
    {
        if (args.Cancelled)
            return;

        args.Cancelled = BlockInteraction(ent, args.User);
    }

    private void OnInsertBlacklistContainerIsInsertingAttempt(Entity<InsertBlacklistComponent> ent, ref ContainerIsInsertingAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        var comp = ent.Comp;

        // Check blacklist
        if (comp.Blacklist != null && _whitelist.IsValid(comp.Blacklist, args.EntityUid) ||
            comp.BlacklistedMobStates != null && TryComp<MobStateComponent>(args.EntityUid, out var blacklistedMobState) && comp.BlacklistedMobStates.Contains(blacklistedMobState.CurrentState))
        {
            args.Cancel();
            return;
        }

        // Check whitelist
        if (comp.Whitelist != null && !_whitelist.IsValid(comp.Whitelist, args.EntityUid) ||
            comp.WhitelistedMobStates != null && TryComp<MobStateComponent>(args.EntityUid, out var whitelistedMobState) && !comp.WhitelistedMobStates.Contains(whitelistedMobState.CurrentState))
        {
            args.Cancel();
        }
    }

    private void OnInRangeOverride(Entity<IgnoreInteractionRangeComponent> ent, ref InRangeOverrideEvent args)
    {
        if (!_whitelist.IsWhitelistPassOrNull(ent.Comp.Whitelist, args.Target))
            return;

        if (!_transform.InRange(args.User, args.Target, ent.Comp.Range))
            return;

        args.InRange = true;
        args.Handled = true;
    }

    public void TryCapWorldRotation(Entity<MaxRotationComponent?, TransformComponent?> max, ref Angle angle)
    {
        if (!Resolve(max, ref max.Comp1, ref max.Comp2, false))
            return;

        var set = max.Comp1.Set;
        var deviation = max.Comp1.Deviation;
        if (Angle.ShortestDistance(angle, set) > deviation)
            angle = set + deviation;

        if (Angle.ShortestDistance(angle, set) < -deviation)
            angle = set - deviation;
    }

    public bool CanFaceMaxRotation(Entity<MaxRotationComponent?, TransformComponent?> max, Angle angle)
    {
        if (!Resolve(max, ref max.Comp1, ref max.Comp2, false))
            return true;

        var set = max.Comp1.Set;
        var deviation = max.Comp1.Deviation;
        if (Angle.ShortestDistance(angle, set) > deviation ||
            Angle.ShortestDistance(angle, set) < -deviation)
        {
            return false;
        }

        return true;
    }

    public void SetMaxRotation(Entity<MaxRotationComponent?> ent, Angle set, Angle deviation)
    {
        ent.Comp ??= EnsureComp<MaxRotationComponent>(ent);
        ent.Comp.Set = set;
        ent.Comp.Deviation = deviation;
        Dirty(ent);
    }
}
