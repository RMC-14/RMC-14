using Content.Shared._RMC14.Evasion;
using Content.Shared.Buckle.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Mobs.Systems;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Robust.Shared.Containers;

namespace Content.Shared._RMC14.Standing;

public sealed class RMCStandingSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly MobStateSystem _mob = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DropItemsOnRestComponent, BuckledEvent>(OnDropBuckled);
        SubscribeLocalEvent<DropItemsOnRestComponent, PickupAttemptEvent>(CancelIfResting);
        SubscribeLocalEvent<DropItemsOnRestComponent, IsEquippingAttemptEvent>(OnDropIsEquippingAttempt);
        SubscribeLocalEvent<DropItemsOnRestComponent, IsUnequippingAttemptEvent>(OnDropIsUnequippingAttempt);
        SubscribeLocalEvent<DropItemsOnRestComponent, AttackAttemptEvent>(CancelIfResting);
        SubscribeLocalEvent<DropItemsOnRestComponent, UseAttemptEvent>(CancelIfResting);

        SubscribeLocalEvent<DownOnEnterComponent, EntInsertedIntoContainerMessage>(OnEnterDown);
        SubscribeLocalEvent<DownOnEnterComponent, EntRemovedFromContainerMessage>(OnLeaveDown);

        SubscribeLocalEvent<StandingStateComponent, EvasionRefreshModifiersEvent>(OnStandingStateEvasionRefresh);
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

    private void OnEnterDown(Entity<DownOnEnterComponent> mob, ref EntInsertedIntoContainerMessage args)
    {
        _standing.Down(args.Entity, false, false, true, true);
    }

    private void OnLeaveDown(Entity<DownOnEnterComponent> mob, ref EntRemovedFromContainerMessage args)
    {
        if (HasComp<KnockedDownComponent>(args.Entity) || _mob.IsIncapacitated(args.Entity))
            _standing.Down(args.Entity, false, true, true, true);
        else
            _standing.Stand(args.Entity);
    }

    private void OnStandingStateEvasionRefresh(Entity<StandingStateComponent> entity, ref EvasionRefreshModifiersEvent args)
    {
        if (entity.Owner != args.Entity.Owner || !_standing.IsDown(entity.Owner, entity.Comp))
            return;

        args.Evasion += (int) EvasionModifiers.Rest;
    }
}
