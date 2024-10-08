using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Wieldable.Components;
using Content.Shared._RMC14.Wieldable.Events;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Timing;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Wieldable;

public sealed class RMCWieldableSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifierSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly UseDelaySystem _useDelaySystem = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    private const string wieldUseDelayID = "RMCWieldDelay";
    private static readonly EntProtoId<SkillDefinitionComponent> WieldSkill = "RMCSkillFirearms";

    public override void Initialize()
    {
        SubscribeLocalEvent<WieldableSpeedModifiersComponent, GotEquippedHandEvent>(OnGotEquippedHand);
        SubscribeLocalEvent<WieldableSpeedModifiersComponent, GotUnequippedHandEvent>(OnGotUnequippedHand);
        SubscribeLocalEvent<WieldableSpeedModifiersComponent, HeldRelayedEvent<RefreshMovementSpeedModifiersEvent>>(OnRefreshMovementSpeedModifiers);
        SubscribeLocalEvent<WieldableSpeedModifiersComponent, ItemUnwieldedEvent>(OnItemUnwielded);
        SubscribeLocalEvent<WieldableSpeedModifiersComponent, ItemWieldedEvent>(OnItemWielded);
        SubscribeLocalEvent<WieldableSpeedModifiersComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<WieldSlowdownCompensationComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<WieldSlowdownCompensationComponent, GotUnequippedEvent>(OnGotUnequipped);

        SubscribeLocalEvent<WieldDelayComponent, GotEquippedHandEvent>(OnGotEquippedHand);
        SubscribeLocalEvent<WieldDelayComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<WieldDelayComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<WieldDelayComponent, ShotAttemptedEvent>(OnShotAttempt);
        SubscribeLocalEvent<WieldDelayComponent, ItemWieldedEvent>(OnItemWieldedWithDelay);


        SubscribeLocalEvent<InventoryComponent, RefreshWieldSlowdownCompensationEvent>(_inventorySystem.RelayEvent);
    }

    private void OnMapInit(Entity<WieldableSpeedModifiersComponent> wieldable, ref MapInitEvent args)
    {
        RefreshSpeedModifiers((wieldable.Owner, wieldable.Comp));
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
            TryComp(transformComponent.ParentUid, out WieldSlowdownCompensationUserComponent? userComponent))
        {
            args.Args.ModifySpeed(
                Math.Min(wieldable.Comp.ModifiedWalk + userComponent.Walk, 1f),
                Math.Min(wieldable.Comp.ModifiedSprint + userComponent.Sprint, 1f));
            return;
        }

        args.Args.ModifySpeed(wieldable.Comp.ModifiedWalk, wieldable.Comp.ModifiedSprint);
    }

    public void RefreshSpeedModifiers(Entity<WieldableSpeedModifiersComponent?> wieldable)
    {
        wieldable.Comp = EnsureComp<WieldableSpeedModifiersComponent>(wieldable);

        var walkSpeed = wieldable.Comp.BaseWalk;
        var sprintSpeed = wieldable.Comp.BaseSprint;

        if (!TryComp(wieldable.Owner, out WieldableComponent? wieldableComponent) || !wieldableComponent.Wielded)
        {
            walkSpeed = 1f;
            sprintSpeed = 1f;
        }

        var ev = new GetWieldableSpeedModifiersEvent(walkSpeed, sprintSpeed);
        RaiseLocalEvent(wieldable, ref ev);

        wieldable.Comp.ModifiedWalk = ev.Walk > 0 ? ev.Walk : 0;
        wieldable.Comp.ModifiedSprint = ev.Sprint > 0 ? ev.Sprint : 0;

        RefreshModifiersOnParent(wieldable.Owner);
    }

    private void OnItemUnwielded(Entity<WieldableSpeedModifiersComponent> wieldable, ref ItemUnwieldedEvent args)
    {
        RefreshSpeedModifiers((wieldable.Owner, wieldable.Comp));
    }

    private void OnItemWielded(Entity<WieldableSpeedModifiersComponent> wieldable, ref ItemWieldedEvent args)
    {
        RefreshSpeedModifiers((wieldable.Owner, wieldable.Comp));
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

#region Wield slowdown compensation
    private void OnGotEquipped(Entity<WieldSlowdownCompensationComponent> armour, ref GotEquippedEvent args)
    {
        EnsureComp(args.Equipee, out WieldSlowdownCompensationUserComponent comp);

        RefreshWieldSlowdownCompensation((args.Equipee, comp));
    }

    private void OnGotUnequipped(Entity<WieldSlowdownCompensationComponent> armour, ref GotUnequippedEvent args)
    {
        EnsureComp(args.Equipee, out WieldSlowdownCompensationUserComponent comp);

        RefreshWieldSlowdownCompensation((args.Equipee, comp));
    }

    private void RefreshWieldSlowdownCompensation(Entity<WieldSlowdownCompensationUserComponent> user)
    {
        var ev = new RefreshWieldSlowdownCompensationEvent(~SlotFlags.POCKET);
        RaiseLocalEvent(user.Owner, ref ev);

        user.Comp.Walk = ev.Walk;
        user.Comp.Walk = ev.Sprint;
    }

    private void OnRefreshWieldSlowdownCompensation(Entity<WieldSlowdownCompensationComponent> armour, ref InventoryRelayedEvent<RefreshWieldSlowdownCompensationEvent> args)
    {
        args.Args.Walk += armour.Comp.Walk;
        args.Args.Sprint += armour.Comp.Sprint;
    }
#endregion

#region Wield delay
    private void OnGotEquippedHand(Entity<WieldDelayComponent> wieldable, ref GotEquippedHandEvent args)
    {
        _useDelaySystem.SetLength(wieldable.Owner, wieldable.Comp.ModifiedDelay, wieldUseDelayID);
        _useDelaySystem.TryResetDelay(wieldable.Owner, id: wieldUseDelayID);
    }

    private void OnUseInHand(Entity<WieldDelayComponent> wieldable, ref UseInHandEvent args)
    {
        if (!TryComp(wieldable.Owner, out UseDelayComponent? useDelayComponent) ||
            !_useDelaySystem.IsDelayed((wieldable.Owner, useDelayComponent), wieldUseDelayID))
        {
            return;
        }

        args.Handled = true;

        if (!_useDelaySystem.TryGetDelayInfo((wieldable.Owner, useDelayComponent), out var info, wieldUseDelayID))
        {
            return;
        }

        var time = $"{(info.EndTime - _timing.CurTime).TotalSeconds:F1}";

        _popupSystem.PopupClient(Loc.GetString("rmc-wield-use-delay", ("seconds", time), ("wieldable", wieldable.Owner)), args.User, args.User);
    }

    public void RefreshWieldDelay(Entity<WieldDelayComponent?> wieldable)
    {
        wieldable.Comp = EnsureComp<WieldDelayComponent>(wieldable);

        var ev = new GetWieldDelayEvent(wieldable.Comp.BaseDelay);
        RaiseLocalEvent(wieldable, ref ev);

        wieldable.Comp.ModifiedDelay = ev.Delay >= TimeSpan.Zero ? ev.Delay : TimeSpan.Zero;
    }

    private void OnItemWieldedWithDelay(Entity<WieldDelayComponent> wieldable, ref ItemWieldedEvent args)
    {
        // TODO RMC14 +0.5s if Dazed
        var skillModifiedDelay = wieldable.Comp.ModifiedDelay;

        if (_container.TryGetContainingContainer((wieldable, null), out var container))
            skillModifiedDelay -= TimeSpan.FromSeconds(0.2) * _skills.GetSkill(container.Owner, WieldSkill);

        _useDelaySystem.SetLength(wieldable.Owner, skillModifiedDelay, wieldUseDelayID);
        _useDelaySystem.TryResetDelay(wieldable.Owner, id: wieldUseDelayID);
    }

    public void OnShotAttempt(Entity<WieldDelayComponent> wieldable, ref ShotAttemptedEvent args)
    {
        if (!TryComp(wieldable.Owner, out UseDelayComponent? useDelayComponent) ||
            !_useDelaySystem.IsDelayed((wieldable.Owner, useDelayComponent), wieldUseDelayID) ||
            !_useDelaySystem.TryGetDelayInfo((wieldable.Owner, useDelayComponent), out var info, wieldUseDelayID))
        {
            return;
        }

        args.Cancel();

        var time = $"{(info.EndTime - _timing.CurTime).TotalSeconds:F1}";

        //_popupSystem.PopupClient(Loc.GetString("rmc-shoot-use-delay", ("seconds", time), ("wieldable", wieldable.Owner)), args.User, args.User);
        // Uncomment when there's a cooldown on popups from a source.
    }

#endregion
}
