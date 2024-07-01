using Content.Shared._RMC14.Armor;
using Content.Shared._RMC14.Wieldable.Components;
using Content.Shared._RMC14.Wieldable.Events;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Timing;
using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Wieldable;

public sealed class RMCWieldableSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifierSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly UseDelaySystem _useDelaySystem = default!;

    private const string wieldUseDelayID = "RMCWieldDelay";

    public override void Initialize()
    {
        SubscribeLocalEvent<WieldableSpeedModifiersComponent, GotEquippedHandEvent>(OnGotEquippedHand);
        SubscribeLocalEvent<WieldableSpeedModifiersComponent, GotUnequippedHandEvent>(OnGotUnequippedHand);
        SubscribeLocalEvent<WieldableSpeedModifiersComponent, HeldRelayedEvent<RefreshMovementSpeedModifiersEvent>>(OnRefreshMovementSpeedModifiers);
        SubscribeLocalEvent<WieldableSpeedModifiersComponent, ItemUnwieldedEvent>(OnItemUnwielded);
        SubscribeLocalEvent<WieldableSpeedModifiersComponent, ItemWieldedEvent>(OnItemWielded);
        SubscribeLocalEvent<WieldableSpeedModifiersComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<WieldDelayComponent, BeforeWieldEvent>(OnBeforeWield);
        SubscribeLocalEvent<WieldDelayComponent, GotEquippedHandEvent>(OnGotEquippedHand);
        SubscribeLocalEvent<WieldDelayComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<WieldableSpeedModifiersComponent> wieldable, ref MapInitEvent args)
    {
        wieldable.Comp.ModifiedWalk = wieldable.Comp.BaseWalk;
        wieldable.Comp.ModifiedSprint = wieldable.Comp.BaseSprint;
    }

    private void OnMapInit(Entity<WieldDelayComponent> wieldable, ref MapInitEvent args)
    {
        wieldable.Comp.ModifiedDelay = wieldable.Comp.BaseDelay;
    }

#region Wield speed modifiers
    private void OnGotEquippedHand(Entity<WieldableSpeedModifiersComponent> wieldable, ref GotEquippedHandEvent args)
    {
        _movementSpeedModifierSystem.RefreshMovementSpeedModifiers(args.User);
    }

    private void OnGotUnequippedHand(Entity<WieldableSpeedModifiersComponent> wieldable, ref GotUnequippedHandEvent args)
    {
        _movementSpeedModifierSystem.RefreshMovementSpeedModifiers(args.User);
    }

    private void OnRefreshMovementSpeedModifiers(Entity<WieldableSpeedModifiersComponent> wieldable, ref HeldRelayedEvent<RefreshMovementSpeedModifiersEvent> args)
    {
        if (TryComp(wieldable.Owner, out TransformComponent? transformComponent) &&
            transformComponent.ParentUid.Valid &&
            TryComp(transformComponent.ParentUid, out CMArmorUserComponent? userComponent))
        {
            args.Args.ModifySpeed(
                Math.Min(wieldable.Comp.ModifiedWalk + userComponent.WieldSlowdownCompensationWalk, 1f), 
                Math.Min(wieldable.Comp.ModifiedSprint + userComponent.WieldSlowdownCompensationSprint, 1f));
            return;
        }

        args.Args.ModifySpeed(wieldable.Comp.ModifiedWalk, wieldable.Comp.ModifiedSprint);
    }
    
    public void RefreshSpeedModifiers(Entity<WieldableSpeedModifiersComponent?> wieldable)
    {
        wieldable.Comp = EnsureComp<WieldableSpeedModifiersComponent>(wieldable);

        var ev = new GetWieldableSpeedModifiersEvent(wieldable.Comp.BaseWalk, wieldable.Comp.BaseSprint);
        RaiseLocalEvent(wieldable, ref ev);

        wieldable.Comp.ModifiedWalk = ev.Walk > 0 ? ev.Walk : 0;
        wieldable.Comp.ModifiedSprint = ev.Sprint > 0 ? ev.Sprint : 0;
        
        RefreshModifiersOnParent(wieldable.Owner);
    }

    private void OnItemUnwielded(Entity<WieldableSpeedModifiersComponent> wieldable, ref ItemUnwieldedEvent args)
    {
        if (args.User == null)
            return;
        
        _movementSpeedModifierSystem.RefreshMovementSpeedModifiers(args.User.Value);
    }

    private void OnItemWielded(Entity<WieldableSpeedModifiersComponent> wieldable, ref ItemWieldedEvent args)
    {
        RefreshModifiersOnParent(wieldable.Owner);
    }
    
    private void RefreshModifiersOnParent(EntityUid wieldableUid)
    {
        if (!TryComp(wieldableUid, out TransformComponent? transformComponent) ||
            !transformComponent.ParentUid.Valid ||
            !TryComp(transformComponent.ParentUid, out HandsComponent? handsComponent) ||
            handsComponent.ActiveHandEntity != wieldableUid)
        {
            return;
        }
        
        _movementSpeedModifierSystem.RefreshMovementSpeedModifiers(transformComponent.ParentUid);
    }
#endregion

#region Wield delay
    private void OnBeforeWield(Entity<WieldDelayComponent> wieldable, ref BeforeWieldEvent args)
    {
        if (!TryComp(wieldable.Owner, out UseDelayComponent? useDelayComponent) ||
            !_useDelaySystem.IsDelayed((wieldable.Owner, useDelayComponent), wieldUseDelayID))
        {
            return;
        }

        args.Cancel();

        if (!_useDelaySystem.TryGetDelayInfo((wieldable.Owner, useDelayComponent), out var info, wieldUseDelayID) ||
            !TryComp(wieldable.Owner, out TransformComponent? transformComponent) ||
            !transformComponent.ParentUid.Valid ||
            !TryComp(transformComponent.ParentUid, out HandsComponent? handsComponent) ||
            handsComponent.ActiveHandEntity != wieldable.Owner)
        {
            return;
        }

        var time = $"{(info.EndTime - _timing.CurTime).TotalSeconds:F1}";

        _popupSystem.PopupClient(Loc.GetString("rmc-wield-use-delay", ("seconds", time), ("wieldable", wieldable.Owner)), transformComponent.ParentUid, transformComponent.ParentUid);
    }

    private void OnGotEquippedHand(Entity<WieldDelayComponent> wieldable, ref GotEquippedHandEvent args)
    {
        _useDelaySystem.SetLength(wieldable.Owner, wieldable.Comp.ModifiedDelay, wieldUseDelayID);
        _useDelaySystem.TryResetDelay(wieldable.Owner, id: wieldUseDelayID);
    }

    public void RefreshWieldDelay(Entity<WieldDelayComponent?> wieldable)
    {
        wieldable.Comp = EnsureComp<WieldDelayComponent>(wieldable);

        var ev = new GetWieldDelayEvent(wieldable.Comp.BaseDelay);
        RaiseLocalEvent(wieldable, ref ev);

        wieldable.Comp.ModifiedDelay = ev.Delay >= TimeSpan.Zero ? ev.Delay : TimeSpan.Zero;
    }
#endregion
}
