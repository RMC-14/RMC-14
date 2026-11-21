using Content.Shared._RMC14.Requisitions.Components;
using Content.Shared.Stacks;
using Content.Shared.Storage;
using Robust.Shared.Containers;

namespace Content.Shared._RMC14.Requisitions;

public sealed class RMCBlackMarketProvenanceSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlackMarketUnsellableComponent, SpawnedFromEntityEvent>(OnSpawnedFromEntity);
        SubscribeLocalEvent<BlackMarketUnsellableComponent, StackSplitEvent>(OnStackSplit);
        SubscribeLocalEvent<BlackMarketUnsellableComponent, StackMergeAttemptEvent>(OnStackMergeAttempt);
    }

    private void OnSpawnedFromEntity(Entity<BlackMarketUnsellableComponent> ent, ref SpawnedFromEntityEvent args)
    {
        MarkUnsellable(args.Spawned);
    }

    private void OnStackSplit(Entity<BlackMarketUnsellableComponent> ent, ref StackSplitEvent args)
    {
        MarkUnsellable(args.NewId);
    }

    private void OnStackMergeAttempt(Entity<BlackMarketUnsellableComponent> ent, ref StackMergeAttemptEvent args)
    {
        if (!HasComp<BlackMarketUnsellableComponent>(args.Other))
            args.Cancelled = true;
    }

    public void MarkUnsellable(EntityUid uid)
    {
        EnsureComp<BlackMarketUnsellableComponent>(uid);

        if (!TryComp(uid, out ContainerManagerComponent? container))
            return;

        foreach (var contained in container.Containers.Values)
        {
            foreach (var entity in contained.ContainedEntities)
            {
                MarkUnsellable(entity);
            }
        }
    }
}
