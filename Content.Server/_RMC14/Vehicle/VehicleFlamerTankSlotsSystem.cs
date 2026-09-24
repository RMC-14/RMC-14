using Content.Shared._RMC14.Vehicle;
using Content.Shared._RMC14.Weapons.Ranged.Flamer;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.Server._RMC14.Vehicle;

public sealed class VehicleFlamerTankSlotsSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<VehicleFlamerTankSlotsComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<VehicleFlamerTankSlotsComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp(ent, out RMCFlamerAmmoProviderComponent? flamerAmmo))
            return;

        var activeSlotId = flamerAmmo.ContainerId;

        for (var i = 0; i < ent.Comp.MaxTanks; i++)
        {
            var slotId = GetSlotId(activeSlotId, i);
            var slot = new ItemSlot { Name = i == 0 ? "Tank" : "Spare Tank" };
            _itemSlots.AddItemSlot(ent, slotId, slot);

            if (ent.Comp.StartingItem is { } startingItem)
            {
                var spawned = Spawn(startingItem, new EntityCoordinates(ent, default));
                _itemSlots.TryInsert(ent, slotId, spawned, null);
            }
        }
    }

    public static string GetSlotId(string activeSlotId, int index)
    {
        return index == 0 ? activeSlotId : $"{activeSlotId}_{index + 1}";
    }
}
