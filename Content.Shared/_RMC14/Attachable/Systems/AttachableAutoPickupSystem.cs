using Content.Shared._RMC14.Attachable.Components;
using Content.Shared._RMC14.Attachable.Events;
using Content.Shared.Containers;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Shared.Containers;

namespace Content.Shared._RMC14.Attachable.Systems;

public sealed class AttachableAutoPickupSystem : EntitySystem
{
    [Dependency] private readonly AttachableHolderSystem _attachableHolderSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AttachableAutoPickupComponent, ContainerGettingRemovedAttemptEvent>(OnGettingRemovedAttempt);
        SubscribeLocalEvent<AttachableAutoPickupComponent, EntGotRemovedFromContainerMessage>(OnGotRemovedFromContainer);
    }

    private void OnGettingRemovedAttempt(Entity<AttachableAutoPickupComponent> attachable, ref ContainerGettingRemovedAttemptEvent args)
    {
        if (attachable.Comp.Removing ||
            attachable.Owner == args.EntityUid ||
            !_attachableHolderSystem.TryGetHolder(attachable.Owner, out var holderUid) ||
            !TryComp(holderUid, out TransformComponent? holderTransformComponent) ||
            !holderTransformComponent.ParentUid.Valid ||
            !_handsSystem.TryGetHand(holderTransformComponent.ParentUid, args.Container.ID, out _) ||
            !_inventorySystem.CanEquip(holderTransformComponent.ParentUid, holderUid.Value, attachable.Comp.SlotId, out _) ||
            !_inventorySystem.TryGetSlotContainer(holderTransformComponent.ParentUid, attachable.Comp.SlotId, out var containerSlot, out _) ||
            containerSlot.Count > 0)
        {
            return;
        }

        args.Cancel();
        attachable.Comp.Removing = true;
        _inventorySystem.TryEquip(holderTransformComponent.ParentUid, holderTransformComponent.ParentUid, holderUid.Value, attachable.Comp.SlotId, silent: true);
    }

    private void OnGotRemovedFromContainer(Entity<AttachableAutoPickupComponent> attachable, ref EntGotRemovedFromContainerMessage args)
    {
        attachable.Comp.Removing = false;
    }
}
