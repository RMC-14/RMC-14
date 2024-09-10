using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Whitelist;

namespace Content.Shared._RMC14.Clothing;

public sealed class RMCClothingSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ClothingRequireEquippedComponent, BeingEquippedAttemptEvent>(OnRequireEquippedBeingEquippedAttempt);

        SubscribeLocalEvent<NoClothingSlowdownComponent, ComponentStartup>(OnNoClothingSlowUpdate);
        SubscribeLocalEvent<NoClothingSlowdownComponent, DidEquipEvent>(OnNoClothingSlowUpdate);
        SubscribeLocalEvent<NoClothingSlowdownComponent, DidUnequipEvent>(OnNoClothingSlowUpdate);
        SubscribeLocalEvent<NoClothingSlowdownComponent, RefreshMovementSpeedModifiersEvent>(OnNoClothingSlowRefresh);
    }

    private void OnNoClothingSlowUpdate<T>(Entity<NoClothingSlowdownComponent> ent, ref T args) where T : EntityEventArgs
    {
        ent.Comp.Active = !_inventory.TryGetSlotEntity(ent, ent.Comp.Slot, out _);
        _movementSpeed.RefreshMovementSpeedModifiers(ent);
    }

    private void OnNoClothingSlowRefresh(Entity<NoClothingSlowdownComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (ent.Comp.Active)
            args.ModifySpeed(ent.Comp.WalkModifier, ent.Comp.SprintModifier);
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
