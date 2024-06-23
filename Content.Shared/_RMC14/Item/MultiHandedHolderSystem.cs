using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Item;

public sealed class MultiHandedHolderSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedVirtualItemSystem _virtualItem = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MultiHandedHolderComponent, PickupAttemptEvent>(OnPickupAttempt);
        SubscribeLocalEvent<MultiHandedHolderComponent, VirtualItemDeletedEvent>(OnVirtualItemDeleted);
        SubscribeLocalEvent<MultiHandedHolderComponent, DidEquipHandEvent>(OnEquipped);
        SubscribeLocalEvent<MultiHandedHolderComponent, DidUnequipHandEvent>(OnUnequipped);
    }

    private void OnPickupAttempt(Entity<MultiHandedHolderComponent> holder, ref PickupAttemptEvent args)
    {
        if (GetHandsNeeded(holder, args.Item) is not { } needed)
            return;

        if (TryComp<HandsComponent>(args.User, out var hands) &&
            hands.CountFreeHands() >= needed)
        {
            return;
        }

        args.Cancel();
        if (_timing.IsFirstTimePredicted)
        {
            _popup.PopupCursor(Loc.GetString("multi-handed-item-pick-up-fail",
                ("number", needed - 1), ("item", args.Item)), args.User);
        }
    }

    private void OnVirtualItemDeleted(Entity<MultiHandedHolderComponent> ent, ref VirtualItemDeletedEvent args)
    {
        if (args.User != ent.Owner)
            return;

        _hands.TryDrop(args.User, args.BlockingEntity);
    }

    private void OnEquipped(Entity<MultiHandedHolderComponent> holder, ref DidEquipHandEvent args)
    {
        if (GetHandsNeeded(holder, args.Equipped) is not { } hands)
            return;

        for (var i = 0; i < hands - 1; i++)
        {
            _virtualItem.TrySpawnVirtualItemInHand(args.Equipped, args.User);
        }
    }

    private void OnUnequipped(Entity<MultiHandedHolderComponent> holder, ref DidUnequipHandEvent args)
    {
        _virtualItem.DeleteInHandsMatching(args.User, args.Unequipped);
    }

    private int? GetHandsNeeded(Entity<MultiHandedHolderComponent> holder, EntityUid item)
    {
        foreach (var (hands, whitelist) in holder.Comp.Items)
        {
            if (_whitelist.IsValid(whitelist, item))
                return hands;
        }

        return null;
    }
}
