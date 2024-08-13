using Content.Shared.Buckle.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Standing;

namespace Content.Shared._RMC14.Standing;

public sealed class RMCStandingSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DropItemsOnRestComponent, BuckledEvent>(OnDropBuckled);
        SubscribeLocalEvent<DropItemsOnRestComponent, PickupAttemptEvent>(CancelIfResting);
        SubscribeLocalEvent<DropItemsOnRestComponent, IsEquippingAttemptEvent>(OnDropIsEquippingAttempt);
        SubscribeLocalEvent<DropItemsOnRestComponent, IsUnequippingAttemptEvent>(OnDropIsUnequippingAttempt);
        SubscribeLocalEvent<DropItemsOnRestComponent, AttackAttemptEvent>(CancelIfResting);
    }

    private void OnDropBuckled(Entity<DropItemsOnRestComponent> drop, ref BuckledEvent args)
    {
        if (!_standing.IsDown(drop))
            return;

        foreach (var hand in _hands.EnumerateHands(drop))
        {
            _hands.TryDrop(drop, hand);
        }
    }

    private void CancelIfResting<T>(Entity<DropItemsOnRestComponent> drop, ref T args) where T : CancellableEntityEventArgs
    {
        TryCancelIfResting(drop, ref args);
    }

    private void OnDropIsEquippingAttempt(Entity<DropItemsOnRestComponent> drop, ref IsEquippingAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (args.Equipee == args.EquipTarget &&
            TryCancelIfResting(drop, ref args))
        {
            args.Reason = "rmc-cant-while-resting";
        }
    }

    private void OnDropIsUnequippingAttempt(Entity<DropItemsOnRestComponent> drop, ref IsUnequippingAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (args.Unequipee == args.UnEquipTarget &&
            TryCancelIfResting(drop, ref args))
        {
            args.Reason = "rmc-cant-while-resting";
        }
    }

    private bool TryCancelIfResting<T>(Entity<DropItemsOnRestComponent> drop, ref T args) where T : CancellableEntityEventArgs
    {
        if (args.Cancelled)
            return false;

        if (_standing.IsDown(drop))
        {
            args.Cancel();
            return true;
        }

        return false;
    }
}
