using Content.Shared.Containers.ItemSlots;
using Content.Shared.Vehicle;
using Content.Shared.Vehicle.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Vehicle;

public sealed class RMCVehicleWheelSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCVehicleWheelSlotsComponent, MapInitEvent>(OnWheelSlotsMapInit);
        SubscribeLocalEvent<RMCVehicleWheelSlotsComponent, EntInsertedIntoContainerMessage>(OnWheelChanged);
        SubscribeLocalEvent<RMCVehicleWheelSlotsComponent, EntRemovedFromContainerMessage>(OnWheelChanged);
        SubscribeLocalEvent<RMCVehicleWheelSlotsComponent, VehicleCanRunEvent>(OnVehicleCanRun);
    }

    private void OnWheelSlotsMapInit(Entity<RMCVehicleWheelSlotsComponent> ent, ref MapInitEvent args)
    {
        var slots = EnsureComp<ItemSlotsComponent>(ent);

        foreach (var slotId in ent.Comp.SlotIds)
        {
            if (_itemSlots.TryGetSlot(ent.Owner, slotId, out _))
                continue;

            var slot = new ItemSlot
            {
                Name = slotId,
                Whitelist = new EntityWhitelist
                {
                    Components = new[] { nameof(RMCVehicleWheelItemComponent) }
                }
            };

            if (ent.Comp.DefaultWheelPrototype != null)
                slot.StartingItem = ent.Comp.DefaultWheelPrototype;

            _itemSlots.AddItemSlot(ent.Owner, slotId, slot, slots);
        }
    }

    private void OnWheelChanged(Entity<RMCVehicleWheelSlotsComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.Owner != ent.Owner)
            return;
    }

    private void OnWheelChanged(Entity<RMCVehicleWheelSlotsComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.Owner != ent.Owner)
            return;
    }

    private void OnVehicleCanRun(Entity<RMCVehicleWheelSlotsComponent> ent, ref VehicleCanRunEvent args)
    {
        if (!AllWheelsPresent(ent))
            args.CanRun = false;
    }

    private bool AllWheelsPresent(Entity<RMCVehicleWheelSlotsComponent> ent)
    {
        if (!TryComp<ItemSlotsComponent>(ent, out var slots))
            return false;

        foreach (var slotId in ent.Comp.SlotIds)
        {
            if (!_itemSlots.TryGetSlot(ent.Owner, slotId, out var slot, slots))
                return false;

            if (slot.ContainerSlot?.ContainedEntity == null)
                return false;
        }

        return true;
    }
}
