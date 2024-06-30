using Content.Shared._RMC14.Attachable.Components;
using Content.Shared._RMC14.Attachable.Events;
using Robust.Shared.Containers;

namespace Content.Shared._RMC14.Attachable.Systems;

public sealed class AttachablePreventDropSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<AttachablePreventDropToggleableComponent, ContainerGettingInsertedAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<AttachablePreventDropToggleableComponent, ContainerGettingRemovedAttemptEvent>(OnAttempt);
    }

    private void OnAttempt<T>(Entity<AttachablePreventDropToggleableComponent> attachable, ref T args) where T : CancellableEntityEventArgs
    {
        if (!TryComp(attachable.Owner, out AttachableToggleableComponent? toggleableComponent) || !toggleableComponent.Attached || !toggleableComponent.Active)
            return;

        args.Cancel();
    }
}
