using Content.Shared._RMC14.Armor;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Wieldable.Components;
using Content.Shared._RMC14.Wieldable.Events;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
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
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly UseDelaySystem _useDelaySystem = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    private const string WieldUseDelayId = "RMCWieldDelay";
    private static readonly EntProtoId<SkillDefinitionComponent> WieldSkill = "RMCSkillFirearms";

    public override void Initialize()
    {
        SubscribeLocalEvent<WieldableSpeedModifiersComponent, GotEquippedHandEvent>(OnGotEquippedHand);
        SubscribeLocalEvent<WieldableSpeedModifiersComponent, GotUnequippedHandEvent>(OnGotUnequippedHand);
        SubscribeLocalEvent<WieldableSpeedModifiersComponent, HeldRelayedEvent<RefreshMovementSpeedModifiersEvent>>(OnRefreshMovementSpeedModifiers);
        SubscribeLocalEvent<WieldableSpeedModifiersComponent, ItemUnwieldedEvent>(OnItemUnwielded);
        SubscribeLocalEvent<WieldableSpeedModifiersComponent, ItemWieldedEvent>(OnItemWielded);
        SubscribeLocalEvent<WieldableSpeedModifiersComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<WieldDelayComponent, GotEquippedHandEvent>(OnGotEquippedHand);
        SubscribeLocalEvent<WieldDelayComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<WieldDelayComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<WieldDelayComponent, ShotAttemptedEvent>(OnShotAttempt);
        SubscribeLocalEvent<WieldDelayComponent, ItemWieldedEvent>(OnItemWieldedWithDelay);
    }

    private void OnMapInit(Entity<WieldableSpeedModifiersComponent> wieldable, ref MapInitEvent args)
    {
        RefreshSpeedModifiers((wieldable.Owner, wieldable.Comp));
    }

    private void OnMapInit(Entity<WieldDelayComponent> wieldable, ref MapInitEvent args)
    {
        wieldable.Comp.ModifiedDelay = wieldable.Comp.BaseDelay;
        Dirty(wieldable);
    }

#region Wield speed modifiers
    private void OnGotEquippedHand(Entity<WieldableSpeedModifiersComponent> wieldable, ref GotEquippedHandEvent args)
    {
        _movementSpeed.RefreshMovementSpeedModifiers(args.User);
    }

    private void OnGotUnequippedHand(Entity<WieldableSpeedModifiersComponent> wieldable, ref GotUnequippedHandEvent args)
    {
        _movementSpeed.RefreshMovementSpeedModifiers(args.User);
    }

    private void OnRefreshMovementSpeedModifiers(Entity<WieldableSpeedModifiersComponent> wieldable, ref HeldRelayedEvent<RefreshMovementSpeedModifiersEvent> args)
    {
        args.Args.ModifySpeed(wieldable.Comp.ModifiedWalk, wieldable.Comp.ModifiedSprint);
    }

    public void RefreshSpeedModifiers(Entity<WieldableSpeedModifiersComponent?> wieldable)
    {
        wieldable.Comp = EnsureComp<WieldableSpeedModifiersComponent>(wieldable);

        var walkSpeed = wieldable.Comp.Base;
        var sprintSpeed = wieldable.Comp.Base;

        if (TryComp(wieldable.Owner, out TransformComponent? transformComponent) &&
            transformComponent.ParentUid.Valid &&
            TryComp(transformComponent.ParentUid, out RMCArmorSpeedTierUserComponent? userComponent))
        {
            switch (userComponent.SpeedTier)
            {
                case "light":
                    walkSpeed = wieldable.Comp.Light;
                    sprintSpeed = wieldable.Comp.Light;
                    break;
                case "medium":
                    walkSpeed = wieldable.Comp.Medium;
                    sprintSpeed = wieldable.Comp.Medium;
                    break;
                case "heavy":
                    walkSpeed = wieldable.Comp.Heavy;
                    sprintSpeed = wieldable.Comp.Heavy;
                    break;
            }
        }

        if (!TryComp(wieldable.Owner, out WieldableComponent? wieldableComponent) || !wieldableComponent.Wielded)
        {
            walkSpeed = 1f;
            sprintSpeed = 1f;
        }

        var ev = new GetWieldableSpeedModifiersEvent(walkSpeed, sprintSpeed);
        RaiseLocalEvent(wieldable, ref ev);

        wieldable.Comp.ModifiedWalk = ev.Walk > 0 ? ev.Walk : 0;
        wieldable.Comp.ModifiedSprint = ev.Sprint > 0 ? ev.Sprint : 0;
        Dirty(wieldable);

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
        if (!TryComp(wieldableUid, out TransformComponent? xform) ||
            !xform.ParentUid.Valid ||
            _hands.GetActiveItem(xform.ParentUid) is not { } active ||
            active != wieldableUid)
        {
            return;
        }

        _movementSpeed.RefreshMovementSpeedModifiers(xform.ParentUid);
    }
#endregion

#region Wield delay
    private void OnGotEquippedHand(Entity<WieldDelayComponent> wieldable, ref GotEquippedHandEvent args)
    {
        _useDelaySystem.SetLength(wieldable.Owner, wieldable.Comp.ModifiedDelay, WieldUseDelayId);
        _useDelaySystem.TryResetDelay(wieldable.Owner, id: WieldUseDelayId);
    }

    private void OnUseInHand(Entity<WieldDelayComponent> wieldable, ref UseInHandEvent args)
    {
        if (!TryComp(wieldable.Owner, out UseDelayComponent? useDelayComponent) ||
            !_useDelaySystem.IsDelayed((wieldable.Owner, useDelayComponent), WieldUseDelayId))
        {
            return;
        }

        args.Handled = true;

        if (!_useDelaySystem.TryGetDelayInfo((wieldable.Owner, useDelayComponent), out var info, WieldUseDelayId))
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
        Dirty(wieldable);
    }

    private void OnItemWieldedWithDelay(Entity<WieldDelayComponent> wieldable, ref ItemWieldedEvent args)
    {
        // TODO RMC14 +0.5s if Dazed
        var skillModifiedDelay = wieldable.Comp.ModifiedDelay;

        if (_container.TryGetContainingContainer((wieldable, null), out var container))
            skillModifiedDelay -= TimeSpan.FromSeconds(0.2) * _skills.GetSkill(container.Owner, WieldSkill);

        _useDelaySystem.SetLength(wieldable.Owner, skillModifiedDelay, WieldUseDelayId);
        _useDelaySystem.TryResetDelay(wieldable.Owner, id: WieldUseDelayId);
    }

    public void OnShotAttempt(Entity<WieldDelayComponent> wieldable, ref ShotAttemptedEvent args)
    {
        if (!wieldable.Comp.PreventFiring)
            return;

        if (!TryComp(wieldable.Owner, out UseDelayComponent? useDelayComponent) ||
            !_useDelaySystem.IsDelayed((wieldable.Owner, useDelayComponent), WieldUseDelayId) ||
            !_useDelaySystem.TryGetDelayInfo((wieldable.Owner, useDelayComponent), out var info, WieldUseDelayId))
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
