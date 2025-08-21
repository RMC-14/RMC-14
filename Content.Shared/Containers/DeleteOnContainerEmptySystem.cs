using Robust.Shared.Containers;
namespace Content.Shared.Containers;

/// <summary>
/// This handles deleting an entity when it's empty
/// </summary>
public sealed class DeleteOnContainerEmptySystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<DeleteOnContainerEmptyComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
    }
    private void OnEntRemoved(Entity<DeleteOnContainerEmptyComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.ContainerId)
            return;

        if (_container.TryGetContainer(ent.Owner, ent.Comp.ContainerId, out var container) && container.ContainedEntities.Count == 0)
            PredictedQueueDel(ent.Owner);
    }
}
