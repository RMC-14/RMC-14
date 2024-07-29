using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Whitelist;

namespace Content.Shared._RMC14.Clothing;

public sealed class RMCClothingSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ClothingRequireEquippedComponent, BeingEquippedAttemptEvent>(OnRequireEquippedBeingEquippedAttempt);
    }

    private void OnRequireEquippedBeingEquippedAttempt(Entity<ClothingRequireEquippedComponent> ent, ref BeingEquippedAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        foreach (var held in _hands.EnumerateHeld(args.EquipTarget))
        {
            if (_whitelist.IsValid(ent.Comp.Whitelist, held))
                return;
        }

        var slots = _inventory.GetSlotEnumerator(args.EquipTarget);
        while (slots.MoveNext(out var slot))
        {
            if (slot.ContainedEntity is not { } contained)
                continue;

            if (_whitelist.IsValid(ent.Comp.Whitelist, contained))
                return;
        }

        args.Cancel();
        args.Reason = ent.Comp.DenyReason;
    }
}
