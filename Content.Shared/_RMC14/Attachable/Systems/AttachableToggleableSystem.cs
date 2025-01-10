using System.Numerics;
using Content.Shared._RMC14.Attachable.Components;
using Content.Shared._RMC14.Attachable.Events;
using Content.Shared._RMC14.Weapons.Ranged;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Actions;
using Content.Shared.Actions.Events;
using Content.Shared.DoAfter;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Light;
using Content.Shared.Movement.Events;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Timing;
using Content.Shared.Toggleable;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Whitelist;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Attachable.Systems;

public sealed class AttachableToggleableSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainerSystem = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookupSystem = default!;
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelistSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaDataSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly AttachableHolderSystem _attachableHolderSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly UseDelaySystem _useDelaySystem = default!;

    private const string attachableToggleUseDelayID = "RMCAttachableToggle";

    private const int bracingInvalidCollisionGroup = (int)CollisionGroup.ThrownItem;
    private const int bracingRequiredCollisionGroup = (int)CollisionGroup.MidImpassable;

    public override void Initialize()
    {
        SubscribeLocalEvent<AttachableToggleableComponent, ActivateInWorldEvent>(OnActivateInWorld,
            after: new[] { typeof(CMGunSystem) });
        SubscribeLocalEvent<AttachableToggleableComponent, AttachableAlteredEvent>(OnAttachableAltered,
            after: new[] { typeof(AttachableModifiersSystem) });
        SubscribeLocalEvent<AttachableToggleableComponent, AttachableToggleableInterruptEvent>(OnAttachableToggleableInterrupt);
        SubscribeLocalEvent<AttachableToggleableComponent, AttachableToggleActionEvent>(OnAttachableToggleAction);
        SubscribeLocalEvent<AttachableToggleableComponent, AttachableToggleDoAfterEvent>(OnAttachableToggleDoAfter);
        SubscribeLocalEvent<AttachableToggleableComponent, AttachableToggleStartedEvent>(OnAttachableToggleStarted);
        SubscribeLocalEvent<AttachableToggleableComponent, AttemptShootEvent>(OnAttemptShoot);
        SubscribeLocalEvent<AttachableToggleableComponent, AttachableRelayedEvent<GunShotEvent>>(OnGunShot);
        SubscribeLocalEvent<AttachableToggleableComponent, GunShotEvent>(OnGunShot);
        SubscribeLocalEvent<AttachableToggleableComponent, ToggleActionEvent>(OnToggleAction,
            before: new[] { typeof(SharedHandheldLightSystem) });
        //SubscribeLocalEvent<AttachableToggleableComponent, UniqueActionEvent>(OnUniqueAction);
        SubscribeLocalEvent<AttachableToggleableComponent, GrantAttachableActionsEvent>(OnGrantAttachableActions);
        SubscribeLocalEvent<AttachableToggleableComponent, RemoveAttachableActionsEvent>(OnRemoveAttachableActions);
        SubscribeLocalEvent<AttachableToggleableComponent, AttachableRelayedEvent<HandDeselectedEvent>>(OnHandDeselected);
        SubscribeLocalEvent<AttachableToggleableComponent, AttachableRelayedEvent<GotEquippedHandEvent>>(OnGotEquippedHand);
        SubscribeLocalEvent<AttachableToggleableComponent, AttachableRelayedEvent<GotUnequippedHandEvent>>(OnGotUnequippedHand);

        SubscribeLocalEvent<AttachableMovementLockedComponent, MoveInputEvent>(OnAttachableMovementLockedMoveInput);

        SubscribeLocalEvent<AttachableToggleableSimpleActivateComponent, AttachableAlteredEvent>(OnAttachableAltered,
            after: new[] { typeof(AttachableModifiersSystem) });

        SubscribeLocalEvent<AttachableToggleablePreventShootComponent, AttachableAlteredEvent>(OnAttachableAltered,
            after: new[] { typeof(AttachableModifiersSystem) });

        SubscribeLocalEvent<AttachableGunPreventShootComponent, AttemptShootEvent>(OnAttemptShoot);
    }

#region AttachableAlteredEvent handling
    private void OnAttachableAltered(Entity<AttachableToggleableComponent> attachable, ref AttachableAlteredEvent args)
    {
        switch (args.Alteration)
        {
            case AttachableAlteredType.Detached:
                if (attachable.Comp.SupercedeHolder &&
                    TryComp(args.Holder, out AttachableHolderComponent? holderComponent) &&
                    holderComponent.SupercedingAttachable == attachable.Owner)
                {
                    _attachableHolderSystem.SetSupercedingAttachable((args.Holder, holderComponent), null);
                }

                if (attachable.Comp.Active)
                {
                    var ev = args with { Alteration = AttachableAlteredType.DetachedDeactivated };
                    RaiseLocalEvent(attachable.Owner, ref ev);
                }

                attachable.Comp.Attached = false;
                attachable.Comp.Active = false;
                Dirty(attachable);
                break;

            case AttachableAlteredType.Attached:
                attachable.Comp.Attached = true;
                break;

            case AttachableAlteredType.Unwielded:
                if (!attachable.Comp.WieldedOnly || !attachable.Comp.Active)
                    break;

                Toggle(attachable, args.User, attachable.Comp.DoInterrupt);
                break;
        }

        if (attachable.Comp.Action == null ||
            !TryComp(attachable.Comp.Action, out InstantActionComponent? actionComponent))
        {
            return;
        }

        _actionsSystem.SetToggled(attachable.Comp.Action, attachable.Comp.Active);
        actionComponent.Enabled = attachable.Comp.Attached;
    }

    private void OnAttachableAltered(Entity<AttachableToggleableSimpleActivateComponent> attachable, ref AttachableAlteredEvent args)
    {
        if (args.User == null)
            return;

        switch (args.Alteration)
        {
            case AttachableAlteredType.Activated:
                RaiseLocalEvent(attachable.Owner, new ActivateInWorldEvent(args.User.Value, args.Holder, true));
                break;

            case AttachableAlteredType.Deactivated:
                RaiseLocalEvent(attachable.Owner, new ActivateInWorldEvent(args.User.Value, args.Holder, true));
                break;

            case AttachableAlteredType.DetachedDeactivated:
                RaiseLocalEvent(attachable.Owner, new ActivateInWorldEvent(args.User.Value, args.Holder, true));
                break;
        }
    }

    private void OnAttachableAltered(Entity<AttachableToggleablePreventShootComponent> attachable, ref AttachableAlteredEvent args)
    {
        if (!TryComp(attachable.Owner, out AttachableToggleableComponent? toggleableComponent))
            return;

        EnsureComp(args.Holder, out AttachableGunPreventShootComponent preventShootComponent);

        switch (args.Alteration)
        {
            case AttachableAlteredType.Attached:
                preventShootComponent.Message = attachable.Comp.Message;
                preventShootComponent.PreventShoot = attachable.Comp.ShootWhenActive && !toggleableComponent.Active || !attachable.Comp.ShootWhenActive && toggleableComponent.Active;
                break;

            case AttachableAlteredType.Detached:
                preventShootComponent.Message = "";
                preventShootComponent.PreventShoot = false;
                break;

            case AttachableAlteredType.Activated:
                preventShootComponent.PreventShoot = !attachable.Comp.ShootWhenActive;
                break;

            case AttachableAlteredType.Deactivated:
                preventShootComponent.PreventShoot = attachable.Comp.ShootWhenActive;
                break;

            case AttachableAlteredType.DetachedDeactivated:
                preventShootComponent.PreventShoot = false;
                break;
        }

        Dirty(args.Holder, preventShootComponent);
    }
#endregion

    private void OnGotEquippedHand(Entity<AttachableToggleableComponent> attachable, ref AttachableRelayedEvent<GotEquippedHandEvent> args)
    {
        if (!attachable.Comp.Attached)
            return;

        args.Args.Handled = true;

        var addEv = new GrantAttachableActionsEvent(args.Args.User);
        RaiseLocalEvent(attachable, ref addEv);
    }

#region Lockouts and interrupts
    private void OnActivateInWorld(Entity<AttachableToggleableComponent> attachable, ref ActivateInWorldEvent args)
    {
        if (attachable.Comp.AttachedOnly && !attachable.Comp.Attached)
            args.Handled = true;
    }

    private void OnAttemptShoot(Entity<AttachableToggleableComponent> attachable, ref AttemptShootEvent args)
    {
        if (args.Cancelled)
            return;

        if (attachable.Comp.AttachedOnly && !attachable.Comp.Attached)
        {
            args.Cancelled = true;
            return;
        }

        if (attachable.Comp.WieldedUseOnly &&
            (!_attachableHolderSystem.TryGetHolder(attachable.Owner, out EntityUid? holderUid) ||
            !TryComp(holderUid, out WieldableComponent? wieldableComponent) ||
            !wieldableComponent.Wielded))
        {
            args.Cancelled = true;

            if (holderUid == null)
                return;

            _popupSystem.PopupClient(
                Loc.GetString("rmc-attachable-shoot-fail-not-wielded", ("holder", holderUid), ("attachable", attachable)),
                args.User,
                args.User);
            return;
        }
    }

    private void OnGunShot(Entity<AttachableToggleableComponent> attachable, ref AttachableRelayedEvent<GunShotEvent> args)
    {
        CheckUserBreakOnRotate(args.Args.User);
        CheckUserBreakOnFullRotate(args.Args.User, args.Args.FromCoordinates, args.Args.ToCoordinates);
    }

    private void OnGunShot(Entity<AttachableToggleableComponent> attachable, ref GunShotEvent args)
    {
        CheckUserBreakOnRotate(args.User);
        CheckUserBreakOnFullRotate(args.User, args.FromCoordinates, args.ToCoordinates);
    }

    private void OnAttemptShoot(Entity<AttachableGunPreventShootComponent> gun, ref AttemptShootEvent args)
    {
        if (args.Cancelled || !gun.Comp.PreventShoot)
            return;

        args.Cancelled = true;

        _popupSystem.PopupClient(gun.Comp.Message, args.User, args.User);
    }

/*    private void OnUniqueAction(Entity<AttachableToggleableComponent> attachable, ref UniqueActionEvent args)
    {
        if (attachable.Comp.AttachedOnly && !attachable.Comp.Attached)
            args.Handled = true;
    }*/

    private void OnHandDeselected(Entity<AttachableToggleableComponent> attachable, ref AttachableRelayedEvent<HandDeselectedEvent> args)
    {
        if (!attachable.Comp.Attached)
            return;

        args.Args.Handled = true;

        if (!attachable.Comp.NeedHand || !attachable.Comp.Active)
            return;

        Toggle(attachable, args.Args.User, attachable.Comp.DoInterrupt);
    }

    private void OnAttachableToggleableInterrupt(Entity<AttachableToggleableComponent> attachable, ref AttachableToggleableInterruptEvent args)
    {
        if (!attachable.Comp.Active)
            return;

        Toggle(attachable, args.User, attachable.Comp.DoInterrupt);
    }

    private void OnGotUnequippedHand(Entity<AttachableToggleableComponent> attachable, ref AttachableRelayedEvent<GotUnequippedHandEvent> args)
    {
        if (!attachable.Comp.Attached)
            return;

        args.Args.Handled = true;

        if (attachable.Comp.NeedHand && attachable.Comp.Active)
            Toggle(attachable, args.Args.User, attachable.Comp.DoInterrupt);


        var removeEv = new RemoveAttachableActionsEvent(args.Args.User);
        RaiseLocalEvent(attachable, ref removeEv);
    }

    private void OnAttachableMovementLockedMoveInput(Entity<AttachableMovementLockedComponent> user, ref MoveInputEvent args)
    {
        if (!args.HasDirectionalMovement)
            return;

        for (var i = user.Comp.AttachableList.Count - 1; i >= 0; i--)
        {
            var attachableUid = user.Comp.AttachableList[i];
            if (!TryComp(attachableUid, out AttachableToggleableComponent? toggleableComponent) ||
                !toggleableComponent.Active ||
                !toggleableComponent.BreakOnMove)
            {
                continue;
            }

            Toggle((attachableUid, toggleableComponent), user.Owner, toggleableComponent.DoInterrupt);
        }
    }

    private void CheckUserBreakOnRotate(Entity<AttachableDirectionLockedComponent?> user)
    {
        if (user.Comp == null)
        {
            if (!TryComp(user.Owner, out AttachableDirectionLockedComponent? lockedComponent))
                return;

            user.Comp = lockedComponent;
        }

        if (Transform(user.Owner).LocalRotation.GetCardinalDir() == user.Comp.LockedDirection)
            return;

        for (var i = user.Comp.AttachableList.Count - 1; i >= 0; i--)
        {
            var attachableUid = user.Comp.AttachableList[i];
            if (!TryComp(attachableUid, out AttachableToggleableComponent? toggleableComponent) ||
                !toggleableComponent.Active ||
                !toggleableComponent.BreakOnRotate)
            {
                continue;
            }

            Toggle((attachableUid, toggleableComponent), user.Owner, toggleableComponent.DoInterrupt);
        }
    }

    private void CheckUserBreakOnFullRotate(Entity<AttachableSideLockedComponent?> user, EntityCoordinates playerPos, EntityCoordinates targetPos)
    {
        if (user.Comp == null)
        {
            if (!TryComp(user.Owner, out AttachableSideLockedComponent? lockedComponent))
                return;

            user.Comp = lockedComponent;
        }

        if (user.Comp.LockedDirection == null)
            return;

        var initialAngle = user.Comp.LockedDirection.Value.ToAngle();
        var playerMapPos = _transformSystem.ToMapCoordinates(playerPos);
        var targetMapPos = _transformSystem.ToMapCoordinates(targetPos);
        var currentAngle = (targetMapPos.Position - playerMapPos.Position).ToWorldAngle();
        
        var differenceFromLockedAngle = (currentAngle.Degrees - initialAngle.Degrees + 180 + 360) % 360 - 180;

        if (differenceFromLockedAngle > -90 && differenceFromLockedAngle < 90)
            return;

        for (var i = user.Comp.AttachableList.Count - 1; i >= 0; i--)
        {
            var attachableUid = user.Comp.AttachableList[i];
            if (!TryComp(attachableUid, out AttachableToggleableComponent? toggleableComponent) ||
                !toggleableComponent.Active ||
                !toggleableComponent.BreakOnFullRotate)
            {
                continue;
            }

            Toggle((attachableUid, toggleableComponent), user.Owner, toggleableComponent.DoInterrupt);
        }
    }
#endregion

#region Toggling
    private void OnAttachableToggleStarted(Entity<AttachableToggleableComponent> attachable, ref AttachableToggleStartedEvent args)
    {
        if (HasComp<XenoComponent>(args.User))
            return;

        if (!CanStartToggleDoAfter(attachable, ref args))
            return;

        var popupText = Loc.GetString(attachable.Comp.Active ? attachable.Comp.DeactivatePopupText : attachable.Comp.ActivatePopupText, ("attachable", attachable.Owner));

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(
            EntityManager,
            args.User,
            GetToggleDoAfter(attachable, args.Holder, args.User, ref popupText),
            new AttachableToggleDoAfterEvent(args.SlotId, popupText),
            attachable,
            target: attachable.Owner,
            used: args.Holder)
        {
            NeedHand = attachable.Comp.DoAfterNeedHand,
            BreakOnMove = attachable.Comp.DoAfterBreakOnMove
        });

        Dirty(attachable);
    }

    private bool CanStartToggleDoAfter(Entity<AttachableToggleableComponent> attachable, ref AttachableToggleStartedEvent args, bool silent = false)
    {
        if (TryComp(attachable.Owner, out UseDelayComponent? useDelayComponent) &&
            _useDelaySystem.IsDelayed((attachable.Owner, useDelayComponent), attachableToggleUseDelayID))
        {
            return false;
        }

        _attachableHolderSystem.TryGetUser(attachable.Owner, out var userUid);

        if (attachable.Comp.HeldOnlyActivate && !attachable.Comp.Active && (userUid == null || !_handsSystem.IsHolding(userUid.Value, args.Holder, out _)))
        {
            if (!silent)
                _popupSystem.PopupClient(
                    Loc.GetString("rmc-attachable-activation-fail-not-held", ("holder", args.Holder), ("attachable", attachable)),
                    args.User,
                    args.User);
            return false;
        }

        if (attachable.Comp.UserOnly && userUid != args.User)
        {
            if (!silent)
                _popupSystem.PopupClient(
                    Loc.GetString("rmc-attachable-activation-fail-not-owned", ("holder", args.Holder), ("attachable", attachable)),
                    args.User,
                    args.User);
            return false;
        }

        if (!attachable.Comp.Active && attachable.Comp.WieldedOnly && (!TryComp(args.Holder, out WieldableComponent? wieldableComponent) || !wieldableComponent.Wielded))
        {
            if (!silent)
                _popupSystem.PopupClient(
                    Loc.GetString("rmc-attachable-activation-fail-not-wielded", ("holder", args.Holder), ("attachable", attachable)),
                    args.User,
                    args.User);
            return false;
        }

        return true;
    }

    private TimeSpan GetToggleDoAfter(Entity<AttachableToggleableComponent> attachable, EntityUid holderUid, EntityUid userUid, ref string popupText)
    {
        if (!TryComp(holderUid, out TransformComponent? transformComponent) || !transformComponent.ParentUid.Valid)
            return TimeSpan.FromSeconds(0f);

        var extraDoAfter = transformComponent.ParentUid == userUid ? 0f : 0.5f;

        switch (attachable.Comp.InstantToggle)
        {
            case AttachableInstantToggleConditions.Brace:
                if (attachable.Comp.Active || transformComponent.ParentUid != userUid || !TryComp(userUid, out TransformComponent? userTransform))
                    break;

                TimeSpan? doAfter;

                var coords = userTransform.Coordinates;

                Func<EntityCoordinates, EntityCoordinates, bool> comparer = (EntityCoordinates userCoords, EntityCoordinates entCoords) => { return false; };
                var coordsShift = new Vector2(0f, 0f);

                Func<HashSet<EntityUid>, EntityUid?> GetBracingSurface = (HashSet<EntityUid> ents) =>
                {
                    foreach (var entity in ents)
                    {
                        if (!TryComp(entity, out FixturesComponent? fixturesComponent))
                            continue;

                        foreach (var fixture in fixturesComponent.Fixtures.Values)
                        {
                            if ((fixture.CollisionLayer & bracingInvalidCollisionGroup) != 0 || (fixture.CollisionLayer & bracingRequiredCollisionGroup) == 0)
                                continue;

                            if (!comparer(coords, Transform(entity).Coordinates))
                                continue;

                            return entity;
                        }
                    }

                    return null;
                };

                switch (userTransform.LocalRotation.GetCardinalDir())
                {
                    case Direction.South:
                        comparer = (EntityCoordinates userCoords, EntityCoordinates entCoords) => { return entCoords.Y < userCoords.Y; };
                        coordsShift = new Vector2(0f, -0.7f);
                        break;

                    case Direction.North:
                        comparer = (EntityCoordinates userCoords, EntityCoordinates entCoords) => { return entCoords.Y > userCoords.Y; };
                        coordsShift = new Vector2(0f, 0.7f);
                        break;

                    case Direction.East:
                        comparer = (EntityCoordinates userCoords, EntityCoordinates entCoords) => { return entCoords.X > userCoords.X; };
                        coordsShift = new Vector2(0.7f, 0f);
                        break;

                    case Direction.West:
                        comparer = (EntityCoordinates userCoords, EntityCoordinates entCoords) => { return entCoords.X < userCoords.X; };
                        coordsShift = new Vector2(-0.7f, 0f);
                        break;

                    default:
                        break;
                }

                var surface = GetBracingSurface(_entityLookupSystem.GetEntitiesInRange(coords, 0.5f, LookupFlags.Dynamic | LookupFlags.Static));
                if (surface != null)
                {
                    popupText = Loc.GetString("attachable-popup-activate-deploy-on-generic", ("attachable", attachable.Owner), ("surface", surface));
                    return TimeSpan.FromSeconds(0f);
                }

                coords = new EntityCoordinates(coords.EntityId, coords.Position + coordsShift);
                surface = GetBracingSurface(_entityLookupSystem.GetEntitiesInRange(coords, 0.5f, LookupFlags.Dynamic | LookupFlags.Static));
                if (surface != null)
                {
                    popupText = Loc.GetString("attachable-popup-activate-deploy-on-generic", ("attachable", attachable.Owner), ("surface", surface));
                    return TimeSpan.FromSeconds(0f);
                }

                popupText = Loc.GetString("attachable-popup-activate-deploy-on-ground", ("attachable", attachable.Owner));
                break;

            default:
                break;
        }

        return TimeSpan.FromSeconds(Math.Max(
            (attachable.Comp.DeactivateDoAfter != null && attachable.Comp.Active
                ? attachable.Comp.DeactivateDoAfter.Value
                : attachable.Comp.DoAfter
            ) + extraDoAfter,
            0));
    }

    private void OnAttachableToggleDoAfter(Entity<AttachableToggleableComponent> attachable,
        ref AttachableToggleDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (args.Target is not { } target || args.Used is not { } used)
            return;

        if (!HasComp<AttachableToggleableComponent>(target))
            return;

        if (!TryComp(args.Used, out AttachableHolderComponent? holderComponent))
            return;

        FinishToggle(attachable, (used, holderComponent), args.SlotId, args.User, args.PopupText);
        args.Handled = true;
        Dirty(attachable);
    }

    private void RemoveUnusedLocks(Entity<AttachableToggleableComponent> attachable, EntityUid? userUid)
    {
        if (userUid == null)
            return;

        if (attachable.Comp.BreakOnMove && TryComp<AttachableMovementLockedComponent>(userUid.Value, out var movementLockedComponent))
        {
            movementLockedComponent.AttachableList.Remove(attachable.Owner);
            if (movementLockedComponent.AttachableList.Count == 0)
                RemCompDeferred<AttachableMovementLockedComponent>(userUid.Value);
        }

        if (attachable.Comp.BreakOnRotate && TryComp<AttachableDirectionLockedComponent>(userUid.Value, out var directionLockedComponent))
        {
            directionLockedComponent.AttachableList.Remove(attachable.Owner);
            if (directionLockedComponent.AttachableList.Count == 0)
                RemCompDeferred<AttachableDirectionLockedComponent>(userUid.Value);
        }

        if (attachable.Comp.BreakOnFullRotate && TryComp<AttachableSideLockedComponent>(userUid.Value, out var sideLockedComponent))
        {
            sideLockedComponent.AttachableList.Remove(attachable.Owner);
            if (sideLockedComponent.AttachableList.Count == 0)
                RemCompDeferred<AttachableSideLockedComponent>(userUid.Value);
        }
    }

    private void FinishToggle(
        Entity<AttachableToggleableComponent> attachable,
        Entity<AttachableHolderComponent> holder,
        string slotId,
        EntityUid? userUid,
        string popupText,
        bool interrupted = false)
    {
        attachable.Comp.Active = !attachable.Comp.Active;

        var mode = attachable.Comp.Active
            ? AttachableAlteredType.Activated
            : interrupted ? AttachableAlteredType.Interrupted : AttachableAlteredType.Deactivated;
        var ev = new AttachableAlteredEvent(holder.Owner, mode, userUid);
        RaiseLocalEvent(attachable.Owner, ref ev);

        var holderEv = new AttachableHolderAttachablesAlteredEvent(attachable.Owner, slotId, mode);
        RaiseLocalEvent(holder.Owner, ref holderEv);

        _useDelaySystem.SetLength(attachable.Owner, attachable.Comp.UseDelay, attachableToggleUseDelayID);
        _useDelaySystem.TryResetDelay(attachable.Owner, id: attachableToggleUseDelayID);
        _actionsSystem.StartUseDelay(attachable.Comp.Action);

        if (attachable.Comp.ShowTogglePopup && userUid != null)
            _popupSystem.PopupClient(popupText, userUid.Value, userUid.Value);

        _audioSystem.PlayPredicted(
            attachable.Comp.Active ? attachable.Comp.ActivateSound : attachable.Comp.DeactivateSound,
            attachable,
            userUid);

        if (!attachable.Comp.Active)
        {
            if (attachable.Comp.SupercedeHolder && holder.Comp.SupercedingAttachable == attachable.Owner)
                _attachableHolderSystem.SetSupercedingAttachable(holder, null);

            RemoveUnusedLocks(attachable, userUid);

            return;
        }

        if (attachable.Comp.BreakOnMove && userUid != null)
        {
            var movementLockedComponent = EnsureComp<AttachableMovementLockedComponent>(userUid.Value);
            movementLockedComponent.AttachableList.Add(attachable.Owner);
        }

        if (attachable.Comp.BreakOnRotate && userUid != null)
        {
            var directionLockedComponent = EnsureComp<AttachableDirectionLockedComponent>(userUid.Value);
            directionLockedComponent.AttachableList.Add(attachable.Owner);

            if (directionLockedComponent.LockedDirection == null)
                directionLockedComponent.LockedDirection = Transform(userUid.Value).LocalRotation.GetCardinalDir();
        }
        
        if (attachable.Comp.BreakOnFullRotate && userUid != null)
        {
            var sideLockedComponent = EnsureComp<AttachableSideLockedComponent>(userUid.Value);
            sideLockedComponent.AttachableList.Add(attachable.Owner);

            if (sideLockedComponent.LockedDirection == null)
                sideLockedComponent.LockedDirection = Transform(userUid.Value).LocalRotation.GetCardinalDir();
        }

        if (!attachable.Comp.SupercedeHolder)
            return;

        if (holder.Comp.SupercedingAttachable != null &&
            TryComp(holder.Comp.SupercedingAttachable, out AttachableToggleableComponent? toggleableComponent))
        {
            toggleableComponent.Active = false;
            ev = new AttachableAlteredEvent(holder.Owner, AttachableAlteredType.Deactivated);
            RaiseLocalEvent(holder.Comp.SupercedingAttachable.Value, ref ev);

            if (_attachableHolderSystem.TryGetSlotId(holder.Owner, attachable.Owner, out var deactivatedSlot))
            {
                holderEv = new AttachableHolderAttachablesAlteredEvent(holder.Comp.SupercedingAttachable.Value,
                    deactivatedSlot,
                    AttachableAlteredType.Deactivated);
                RaiseLocalEvent(holder.Owner, ref holderEv);
            }
        }

        _attachableHolderSystem.SetSupercedingAttachable(holder, attachable.Owner);
    }

    private void Toggle(Entity<AttachableToggleableComponent> attachable, EntityUid? user, bool interrupted = false)
    {
        if (!_attachableHolderSystem.TryGetHolder(attachable.Owner, out var holderUid) ||
            !TryComp(holderUid, out AttachableHolderComponent? holderComponent) ||
            !_attachableHolderSystem.TryGetSlotId(holderUid.Value, attachable.Owner, out var slotId))
        {
            return;
        }

        FinishToggle(
            attachable,
            (holderUid.Value, holderComponent),
            slotId,
            user,
            Loc.GetString(attachable.Comp.Active ? attachable.Comp.DeactivatePopupText : attachable.Comp.ActivatePopupText, ("attachable", attachable.Owner)),
            interrupted);
        Dirty(attachable);
    }
#endregion

#region Actions
    private void OnGrantAttachableActions(Entity<AttachableToggleableComponent> ent, ref GrantAttachableActionsEvent args)
    {
        GrantAttachableActions(ent, args.User);
        RelayAttachableActions(ent, args.User);
    }

    private void GrantAttachableActions(Entity<AttachableToggleableComponent> ent, EntityUid user, bool doSecondTry = true)
    {
        // This is to prevent ActionContainerSystem from shitting itself if the attachment has actions other than its attachment toggle.
        if (!TryComp(ent.Owner, out ActionsContainerComponent? actionsContainerComponent) || actionsContainerComponent.Container == null)
        {
            EnsureComp<ActionsContainerComponent>(ent.Owner);

            if (doSecondTry)
                GrantAttachableActions(ent, user, false);

            return;
        }

        var exists = ent.Comp.Action != null;
        _actionContainerSystem.EnsureAction(ent, ref ent.Comp.Action, ent.Comp.ActionId, actionsContainerComponent);

        if (ent.Comp.Action is not { } actionId)
            return;

        _actionsSystem.GrantContainedAction(user, ent.Owner, actionId);

        if (exists)
            return;

        _metaDataSystem.SetEntityName(actionId, ent.Comp.ActionName);
        _metaDataSystem.SetEntityDescription(actionId, ent.Comp.ActionDesc);

        if (_actionsSystem.TryGetActionData(actionId, out var action))
        {
            action.Icon = ent.Comp.Icon;
            action.IconOn = ent.Comp.IconActive;
            action.Enabled = ent.Comp.Attached;
            action.UseDelay = ent.Comp.UseDelay;
            Dirty(actionId, action);
        }

        Dirty(ent);
    }

    private void RelayAttachableActions(Entity<AttachableToggleableComponent> attachable, EntityUid user)
    {
        if (attachable.Comp.ActionsToRelayWhitelist == null || !TryComp(attachable.Owner, out ActionsContainerComponent? actionsContainerComponent))
            return;

        foreach (var actionUid in actionsContainerComponent.Container.ContainedEntities)
        {
            if (!_entityWhitelistSystem.IsWhitelistPass(attachable.Comp.ActionsToRelayWhitelist, actionUid))
                continue;

            _actionsSystem.GrantContainedAction(user, (attachable.Owner, actionsContainerComponent), actionUid);
        }
    }

    private void OnRemoveAttachableActions(Entity<AttachableToggleableComponent> ent, ref RemoveAttachableActionsEvent args)
    {
        RemoveAttachableActions(ent, args.User);
        RemoveRelayedActions(ent, args.User);
    }

    private void RemoveAttachableActions(Entity<AttachableToggleableComponent> ent, EntityUid user)
    {
        if (ent.Comp.Action is not { } action)
            return;

        if (!TryComp(action, out InstantActionComponent? actionComponent) || actionComponent.AttachedEntity != user)
            return;

        _actionsSystem.RemoveProvidedAction(user, ent, action);
    }

    private void RemoveRelayedActions(Entity<AttachableToggleableComponent> attachable, EntityUid user)
    {
        if (attachable.Comp.ActionsToRelayWhitelist == null || !TryComp(attachable.Owner, out ActionsContainerComponent? actionsContainerComponent))
            return;

        foreach (var actionUid in actionsContainerComponent.Container.ContainedEntities)
        {
            if (!_entityWhitelistSystem.IsWhitelistPass(attachable.Comp.ActionsToRelayWhitelist, actionUid))
                continue;

            _actionsSystem.RemoveProvidedAction(user, attachable.Owner, actionUid);
        }
    }

    private void OnAttachableToggleAction(Entity<AttachableToggleableComponent> attachable,
        ref AttachableToggleActionEvent args)
    {
        args.Handled = true;

        if (!attachable.Comp.Attached)
            return;

        if (!_attachableHolderSystem.TryGetHolder(attachable.Owner, out var holderUid) ||
            !TryComp(holderUid, out AttachableHolderComponent? holderComponent) ||
            !_attachableHolderSystem.TryGetSlotId(holderUid.Value, attachable.Owner, out var slotId))
        {
            return;
        }

        var ev = new AttachableToggleStartedEvent(holderUid.Value,
            args.Performer,
            slotId);
        RaiseLocalEvent(attachable.Owner, ref ev);
    }

    private void OnToggleAction(Entity<AttachableToggleableComponent> attachable, ref ToggleActionEvent args)
    {
        if (attachable.Comp.AttachedOnly && !attachable.Comp.Attached)
            args.Handled = true;
    }
#endregion
}
