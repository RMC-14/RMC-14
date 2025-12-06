using Content.Shared._RMC14.Armor;
using Content.Shared._RMC14.UniformAccessories;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Shared.Containers;

namespace Content.Shared._RMC14.UniformAccessories;

public sealed class SharedUniformAccessoryEquipBlockSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _containers = default!;

    private const string MaskCategory = "Mask";

    public override void Initialize()
    {
        SubscribeLocalEvent<UniformAccessoryHolderComponent, InventoryRelayedEvent<RMCEquipAttemptEvent>>(OnEquipAttempt);
    }

    private void OnEquipAttempt(Entity<UniformAccessoryHolderComponent> ent, ref InventoryRelayedEvent<RMCEquipAttemptEvent> args)
    {
        ref readonly var ev = ref args.Args.Event;

        if (ev.Cancelled)
            return;

        // Only care about mask/neck slots.
        if ((ev.SlotFlags & (SlotFlags.MASK | SlotFlags.NECK)) == 0)
            return;

        if (!_containers.TryGetContainer(ent.Owner, ent.Comp.ContainerId, out var container))
            return;

        foreach (var contained in container.ContainedEntities)
        {
            if (!TryComp<UniformAccessoryComponent>(contained, out var accessory))
                continue;

            if (accessory.Category != MaskCategory)
                continue;

            ev.Cancel();
            ev.Reason = "inventory-component-can-equip-does-not-fit";

            break;
        }
    }
}
