using Content.Shared._RMC14.Evasion;
using Content.Shared._RMC14.Input;
using Content.Shared.ActionBlocker;
using Content.Shared.Buckle.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Pulling.Events;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Robust.Shared.Containers;
using Robust.Shared.Input.Binding;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Standing;

public sealed class RMCStandingSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly MobStateSystem _mob = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

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

        SubscribeLocalEvent<RMCRestComponent, StoodEvent>(OnRestStood);
        SubscribeLocalEvent<RMCRestComponent, StandAttemptEvent>(OnRestStandAttempt);
        SubscribeLocalEvent<RMCRestComponent, StartPullAttemptEvent>(OnRestStartPullAttempt);

        CommandBinds.Builder
            .Bind(CMKeyFunctions.RMCRest,
                InputCmdHandler.FromDelegate(session =>
                    {
                        if (session?.AttachedEntity is not { } ent)
                            return;

                        if (!TryComp(ent, out RMCRestComponent? rest))
                            return;

                        var time = _timing.CurTime;
                        if (time < rest.LastToggleAt + rest.Cooldown)
                            return;

                        if (rest.Resting)
                        {
                            SetRest((ent, rest), false);

                            if (_standing.IsDown(ent))
                                _popup.PopupClient(Loc.GetString("rmc-standing-stand-when-able"), ent, ent, PopupType.Medium);
                        }
                        else
                        {
                            if (!_actionBlocker.CanInteract(ent, null))
                                return;

                            if (_standing.IsDown(ent))
                                _popup.PopupClient(Loc.GetString("rmc-standing-keep-lying"), ent, ent, PopupType.Medium);

                            rest.Resting = true;
                            Dirty(ent, rest);
                            _standing.Down(ent);
                        }

                        rest.LastToggleAt = time;
                        _movementSpeed.RefreshMovementSpeedModifiers(ent);
                    },
                    handle: false))
            .Register<RMCStandingSystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        CommandBinds.Unregister<RMCStandingSystem>();
    }

    private void OnDropBuckled(Entity<DropItemsOnRestComponent> drop, ref BuckledEvent args)
    {
        if (!_standing.IsDown(drop))
            return;

        foreach (var held in _hands.EnumerateHeld(drop.Owner))
        {
            _hands.TryDrop(drop.Owner, held);
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

    private void OnRestStood(Entity<RMCRestComponent> ent, ref StoodEvent args)
    {
        ent.Comp.Resting = false;
        Dirty(ent);
    }

    private void OnRestStandAttempt(Entity<RMCRestComponent> ent, ref StandAttemptEvent args)
    {
        if (ent.Comp.Resting)
            args.Cancel();
    }

    private void OnRestStartPullAttempt(Entity<RMCRestComponent> ent, ref StartPullAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (args.Puller != ent.Owner)
            return;

        if (!ent.Comp.Resting)
            return;

        args.Cancel();
    }

    public void SetRest(Entity<RMCRestComponent?> rest, bool resting)
    {
        if (!Resolve(rest, ref rest.Comp, false))
            return;

        rest.Comp.Resting = resting;
        Dirty(rest);

        if (!resting)
            _standing.Stand(rest);
    }
}
