using Content.Shared.Interaction.Events;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;

namespace Content.Shared._RMC14.Interaction;

public sealed class RMCInteractionSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<InteractedBlacklistComponent, GettingInteractedWithAttemptEvent>(OnBlacklistInteractionAttempt);
        SubscribeLocalEvent<InsertBlacklistComponent, ContainerGettingInsertedAttemptEvent>(OnInsertBlacklistContainerInsertedAttempt);
    }

    private void OnBlacklistInteractionAttempt(Entity<InteractedBlacklistComponent> ent, ref GettingInteractedWithAttemptEvent args)
    {
        if (args.Cancelled || ent.Comp.Blacklist == null)
            return;

        if (_whitelist.IsValid(ent.Comp.Blacklist, args.Uid))
            args.Cancelled = true;
    }

    private void OnInsertBlacklistContainerInsertedAttempt(Entity<InsertBlacklistComponent> ent, ref ContainerGettingInsertedAttemptEvent args)
    {
        if (args.Cancelled || ent.Comp.Blacklist is not { } blacklist)
            return;

        if (_whitelist.IsValid(blacklist, args.EntityUid))
            args.Cancel();
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
