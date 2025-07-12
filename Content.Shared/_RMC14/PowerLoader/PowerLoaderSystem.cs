using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.Dropship.AttachmentPoint;
using Content.Shared._RMC14.Dropship.Fabricator;
using Content.Shared._RMC14.Dropship.Utility.Components;
using Content.Shared._RMC14.Dropship.Weapon;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.PowerLoader.Events;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Coordinates;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Item;
using Content.Shared.Mobs;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using static Content.Shared.Interaction.SharedInteractionSystem;
using static Content.Shared.Physics.CollisionGroup;
using DropshipUtilityComponent = Content.Shared._RMC14.Dropship.Utility.Components.DropshipUtilityComponent;

namespace Content.Shared._RMC14.PowerLoader;

public sealed class PowerLoaderSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedDropshipSystem _dropship = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly SharedMoverController _mover = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedVirtualItemSystem _virtualItem = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    private EntityQuery<PowerLoaderGrabbableComponent> _powerLoaderGrabbableQuery;

    public override void Initialize()
    {
        _powerLoaderGrabbableQuery = GetEntityQuery<PowerLoaderGrabbableComponent>();

        SubscribeLocalEvent<PowerLoaderComponent, MapInitEvent>(OnPowerLoaderMapInit);
        SubscribeLocalEvent<PowerLoaderComponent, ComponentRemove>(OnPowerLoaderRemove);
        SubscribeLocalEvent<PowerLoaderComponent, EntityTerminatingEvent>(OnPowerLoaderTerminating);
        SubscribeLocalEvent<PowerLoaderComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
        SubscribeLocalEvent<PowerLoaderComponent, StrapAttemptEvent>(OnStrapAttempt);
        SubscribeLocalEvent<PowerLoaderComponent, StrappedEvent>(OnStrapped);
        SubscribeLocalEvent<PowerLoaderComponent, UnstrappedEvent>(OnUnstrapped);
        SubscribeLocalEvent<PowerLoaderComponent, PowerLoaderGrabDoAfterEvent>(OnGrabDoAfter);
        SubscribeLocalEvent<PowerLoaderComponent, GetUsedEntityEvent>(OnGetUsedEntity);
        SubscribeLocalEvent<PowerLoaderComponent, UserActivateInWorldEvent>(OnUserGrab);
        SubscribeLocalEvent<PowerLoaderComponent, DestructionEventArgs>(OnDestruction);

        SubscribeLocalEvent<PowerLoaderGrabbableComponent, PickupAttemptEvent>(OnGrabbablePickupAttempt);
        SubscribeLocalEvent<PowerLoaderGrabbableComponent, AfterInteractEvent>(OnGrabbableAfterInteract);
        SubscribeLocalEvent<PowerLoaderGrabbableComponent, CombatModeShouldHandInteractEvent>(OnGrababbleShouldInteract);
        SubscribeLocalEvent<PowerLoaderGrabbableComponent, BeforeRangedInteractEvent>(OnGrabbableBeforeRangedInteract);

        // Detach events and doAfters
        SubscribeLocalEvent<DropshipWeaponPointComponent, ActivateInWorldEvent>(OnPointActivateInWorld);
        SubscribeLocalEvent<DropshipUtilityPointComponent, ActivateInWorldEvent>(OnPointActivateInWorld);
        SubscribeLocalEvent<DropshipEnginePointComponent, ActivateInWorldEvent>(OnEngineActivateInWorld);

        SubscribeLocalEvent<DropshipWeaponPointComponent, DropshipDetachDoAfterEvent>(OnDropshipDetach);
        SubscribeLocalEvent<DropshipUtilityPointComponent, DropshipDetachDoAfterEvent>(OnDropshipDetach);
        SubscribeLocalEvent<DropshipEnginePointComponent, DropshipDetachDoAfterEvent>(OnEngineDetach);

        // Attach events and doAfters
        SubscribeLocalEvent<DropshipWeaponPointComponent, GetAttachmentSlotEvent>(OnGetSlot);
        SubscribeLocalEvent<DropshipUtilityPointComponent, GetAttachmentSlotEvent>(OnGetSlot);
        SubscribeLocalEvent<DropshipEnginePointComponent, GetAttachmentSlotEvent>(OnGetSlot);

        SubscribeLocalEvent<DropshipWeaponPointComponent, DropshipAttachDoAfterEvent>(OnDropshipAttach);
        SubscribeLocalEvent<DropshipUtilityPointComponent, DropshipAttachDoAfterEvent>(OnDropshipAttach);
        SubscribeLocalEvent<DropshipEnginePointComponent, DropshipAttachDoAfterEvent>(OnDropshipAttach);

        SubscribeLocalEvent<DropshipFabricatorPrintableComponent, PowerLoaderInteractEvent>(OnDropshipPartPowerLoaderInteract);

        SubscribeLocalEvent<ActivePowerLoaderPilotComponent, PreventCollideEvent>(OnActivePilotPreventCollide);
        SubscribeLocalEvent<ActivePowerLoaderPilotComponent, KnockedDownEvent>(OnActivePilotStunned);
        SubscribeLocalEvent<ActivePowerLoaderPilotComponent, StunnedEvent>(OnActivePilotStunned);
        SubscribeLocalEvent<ActivePowerLoaderPilotComponent, MobStateChangedEvent>(OnActivePilotMobStateChanged);
    }

    private void OnPowerLoaderMapInit(Entity<PowerLoaderComponent> ent, ref MapInitEvent args)
    {
        _container.EnsureContainer<Container>(ent, ent.Comp.VirtualContainerId);
    }

    private void OnPowerLoaderRemove(Entity<PowerLoaderComponent> ent, ref ComponentRemove args)
    {
        RemoveLoader(ent);
    }

    private void OnPowerLoaderTerminating(Entity<PowerLoaderComponent> ent, ref EntityTerminatingEvent args)
    {
        RemoveLoader(ent);
    }

    private void OnRefreshSpeed(Entity<PowerLoaderComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (!TryComp(ent, out StrapComponent? strap))
            return;

        var highestSkill = 0;
        foreach (var buckled in strap.BuckledEntities)
        {
            var skill = _skills.GetSkill(buckled, ent.Comp.SpeedSkill);
            if (skill > highestSkill)
                highestSkill = skill;
        }

        if (highestSkill <= 0)
            return;

        var speed = ent.Comp.SpeedPerSkill * highestSkill;
        args.ModifySpeed(speed, speed);
    }

    private void OnStrapAttempt(Entity<PowerLoaderComponent> ent, ref StrapAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        var buckle = args.Buckle;
        if (!_skills.HasSkills(buckle.Owner, ent.Comp.Skills))
        {
            if (args.Popup)
                _popup.PopupClient(Loc.GetString("rmc-skills-cant-operate", ("target", ent)), buckle, args.User);

            args.Cancelled = true;
            return;
        }

        if (_hands.CountFreeHands(buckle.Owner) < 2)
        {
            if (args.Popup)
                _popup.PopupClient(Loc.GetString("rmc-power-loader-hands-occupied", ("mech", ent)), buckle, args.User);

            args.Cancelled = true;
        }
    }

    private void OnStrapped(Entity<PowerLoaderComponent> ent, ref StrappedEvent args)
    {
        var buckle = args.Buckle;
        var relay = EnsureComp<InteractionRelayComponent>(buckle);
        EnsureComp<ActivePowerLoaderPilotComponent>(buckle);

        _mover.SetRelay(buckle, ent);
        _interaction.SetRelay(buckle, ent, relay);
        _movementSpeed.RefreshMovementSpeedModifiers(ent);
        SyncHands(ent);
    }

    private void OnUnstrapped(Entity<PowerLoaderComponent> ent, ref UnstrappedEvent args)
    {
        var buckle = args.Buckle;
        RemCompDeferred<ActivePowerLoaderPilotComponent>(buckle);
        RemCompDeferred<RelayInputMoverComponent>(buckle);
        RemCompDeferred<InteractionRelayComponent>(buckle);

        _movementSpeed.RefreshMovementSpeedModifiers(ent);
        DeleteVirtuals(ent, buckle);

        if (ent.Comp.DoAfter != null && _doAfter.IsRunning(ent.Comp.DoAfter.Id))
            _doAfter.Cancel(ent.Comp.DoAfter.Id);
    }

    private void OnGrabDoAfter(Entity<PowerLoaderComponent> ent, ref PowerLoaderGrabDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target)
            return;

        PickUp(ent, target);
    }

    private void OnGetUsedEntity(Entity<PowerLoaderComponent> ent, ref GetUsedEntityEvent args)
    {
        foreach (var buckled in GetBuckled(ent))
        {
            if (!TryComp(buckled, out HandsComponent? hands))
                continue;

            if (!_hands.TryGetActiveItem((buckled, hands), out var held) ||
                !TryComp(held, out VirtualItemComponent? virtualItem) ||
                !TryComp(virtualItem.BlockingEntity, out PowerLoaderVirtualItemComponent? item))
            {
                continue;
            }

            foreach (var contained in _hands.EnumerateHeld(ent.Owner))
            {
                if (contained == item.Grabbed)
                {
                    args.Used = item.Grabbed;
                    return;
                }
            }

            args.Used = null;
            break;
        }
    }

    private void OnUserGrab(Entity<PowerLoaderComponent> ent, ref UserActivateInWorldEvent args)
    {
        if (!TryComp(ent, out StrapComponent? strap))
            return;

        var grab = new PowerLoaderGrabEvent(ent, args.Target, strap.BuckledEntities);
        RaiseLocalEvent(args.Target, ref grab);

        if (grab.ToGrab != null)
        {
            PickUp(ent, grab.ToGrab.Value);
            return;
        }

        if (!CanPickupPopup(ent, args.Target, out var delay))
            return;

        var ev = new PowerLoaderGrabDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, ent, delay, ev, ent, args.Target)
        {
            BreakOnMove = true,
            DuplicateCondition = DuplicateConditions.SameEvent,
        };

        if (_doAfter.TryStartDoAfter(doAfter))
            ent.Comp.DoAfter = ev.DoAfter;
    }

    private void OnDestruction(Entity<PowerLoaderComponent> ent, ref DestructionEventArgs args)
    {
        var held = _hands.EnumerateHeld(ent.Owner).ToList();
        foreach (var item in held)
        {
            _hands.TryDrop(ent.Owner, item);
        }
    }

    private void OnPointActivateInWorld(Entity<DropshipWeaponPointComponent> ent, ref ActivateInWorldEvent args)
    {
        if (!TryComp(args.User, out PowerLoaderComponent? loader))
            return;

        args.Handled = true;

        var user = new Entity<PowerLoaderComponent?>(args.User, loader);
        var target = args.Target;
        ContainerSlot container;

        if (CanDetachPopup(ref user, ent, ent.Comp.AmmoContainerSlotId, false, out var ammoSlot) &&
            ammoSlot.ContainedEntity != null)
        {
            container = ammoSlot;
        }
        else if (CanDetachPopup(ref user, ent, ent.Comp.WeaponContainerSlotId, true, out var weaponSlot) &&
                 weaponSlot.ContainedEntity != null)
        {
            container = weaponSlot;
        }
        else
        {
            return;
        }

        StartPointDetach(ent, container, (user, loader), target);
    }

    private void OnPointActivateInWorld(Entity<DropshipUtilityPointComponent> ent, ref ActivateInWorldEvent args)
    {
        TryStartPointDetach(ent, ent.Comp.UtilitySlotId, ref args);
    }

    private void OnEngineActivateInWorld(Entity<DropshipEnginePointComponent> ent, ref ActivateInWorldEvent args)
    {
        TryStartPointDetach(ent, ent.Comp.ContainerId, ref args);
    }

    private void OnGrabbablePickupAttempt(Entity<PowerLoaderGrabbableComponent> ent, ref PickupAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        // TODO RMC14 popup
        if (!HasComp<PowerLoaderComponent>(args.User))
            args.Cancel();
    }

    private void OnGrabbableAfterInteract(Entity<PowerLoaderGrabbableComponent> ent, ref AfterInteractEvent args)
    {
        var user = args.User;
        if (!TryComp(user, out PowerLoaderComponent? loader))
            return;

        if (!_hands.IsHolding(user, ent))
            return;

        var source = ent.Owner.ToCoordinates();
        var coords = _transform.GetMoverCoordinates(args.ClickLocation);
        coords = coords.SnapToGrid(EntityManager, _mapManager);
        if (!source.TryDistance(EntityManager, coords, out var distance))
            return;

        args.Handled = true;
        if (distance < 0.5f)
        {
            var msg = Loc.GetString("rmc-power-loader-too-close");
            foreach (var buckled in GetBuckled(user))
            {
                _popup.PopupClient(msg, ent, buckled, PopupType.SmallCaution);
            }

            return;
        }

        if (distance > InteractionRange)
        {
            var msg = Loc.GetString("rmc-power-loader-too-far");
            foreach (var buckled in GetBuckled(user))
            {
                _popup.PopupClient(msg, ent, buckled, PopupType.SmallCaution);
            }

            return;
        }

        var group = Impassable | MidImpassable | HighImpassable | InteractImpassable | MobLayer;
        if (_rmcMap.IsTileBlocked(coords, group) ||
            _rmcMap.TileHasStructure(coords))
        {
            var msg = Loc.GetString("rmc-power-loader-cant-drop-occupied", ("drop", ent));
            foreach (var buckled in GetBuckled(user))
            {
                _popup.PopupClient(msg, ent, buckled, PopupType.SmallCaution);
            }

            return;
        }

        var used = args.Used;
        if (_hands.TryDrop(user, used, coords, false))
        {
            _transform.AnchorEntity((used, Transform(used)));
            SyncHands((user, loader));
        }
    }

    private void OnGetSlot(Entity<DropshipWeaponPointComponent> ent, ref GetAttachmentSlotEvent args)
    {
        var user = new Entity<PowerLoaderComponent?>(GetEntity(args.User), null);
        if (!TryGetEntity(args.Used, out var used))
            return;

        ContainerSlot? slot;
        if (args.BeingAttached)
        {
            args.CanUse = CanAttachPopup(ref user, ent, used.Value, out slot);
        }
        else
        {
            args.CanUse = CanDetachPopup(ref user, ent, ent.Comp.AmmoContainerSlotId, false, out slot) ||
                          CanDetachPopup(ref user, ent, ent.Comp.WeaponContainerSlotId, false, out slot);
        }

        if (slot != null)
            args.SlotId = slot.ID;
    }

    private void OnGetSlot(Entity<DropshipUtilityPointComponent> ent, ref GetAttachmentSlotEvent args)
    {
        TryGetSlot(ent, ent.Comp.UtilitySlotId, ref args);
    }

    private void OnGetSlot(Entity<DropshipEnginePointComponent> ent, ref GetAttachmentSlotEvent args)
    {
        TryGetSlot(ent, ent.Comp.ContainerId, ref args);
    }

    private void OnDropshipAttach(Entity<DropshipWeaponPointComponent> ent, ref DropshipAttachDoAfterEvent args)
    {
        if (!TryGetPointContainer(args, out var user, out _, out var contained, out var slot))
            return;

        InsertPoint(user, contained, slot);
        SyncAppearance(ent);
    }

    private void OnDropshipAttach(Entity<DropshipUtilityPointComponent> ent, ref DropshipAttachDoAfterEvent args)
    {
        if (!TryGetPointContainer(args, out var user, out var container, out var contained, out var slot))
            return;

        if (TryComp(contained, out DropshipUtilityComponent? utilityComp))
            utilityComp.AttachmentPoint = container;

        InsertPoint(user, contained, slot);
        SyncAppearance(ent, ent.Comp.UtilitySlotId);
    }

    private void OnDropshipAttach(Entity<DropshipEnginePointComponent> ent, ref DropshipAttachDoAfterEvent args)
    {
        if (!TryGetPointContainer(args, out var user, out _, out var contained, out var slot))
            return;

        InsertPoint(user, contained, slot);
        SyncAppearance(ent, ent.Comp.ContainerId);
    }

    private void OnDropshipDetach(Entity<DropshipWeaponPointComponent> ent, ref DropshipDetachDoAfterEvent args)
    {
        if (!TryGetPointContainer(args, out var user, out _, out var contained, out var slot))
            return;

        _container.Remove(contained, slot);

        if (TryComp(contained, out DropshipAmmoComponent? ammo) &&
            ammo.Rounds < ammo.RoundsPerShot)
        {
            QueueDel(contained);

            var msg = Loc.GetString("rmc-power-loader-discard-empty", ("ammo", contained));
            foreach (var buckled in GetBuckled(user))
            {
                _popup.PopupClient(msg, buckled, PopupType.Medium);
            }
        }
        else
        {
            PickUp((user, user.Comp), contained);
            SyncHands((user, user.Comp));
        }

        SyncAppearance(ent);
    }

    private void OnDropshipDetach(Entity<DropshipUtilityPointComponent> ent, ref DropshipDetachDoAfterEvent args)
    {
        DetachPoint(ref args);
        SyncAppearance(ent, ent.Comp.UtilitySlotId);
    }

    private void OnEngineDetach(Entity<DropshipEnginePointComponent> ent, ref DropshipDetachDoAfterEvent args)
    {
        DetachPoint(ref args);
        SyncAppearance(ent, ent.Comp.ContainerId);
    }

    private void OnGrababbleShouldInteract(Entity<PowerLoaderGrabbableComponent> ent, ref CombatModeShouldHandInteractEvent args)
    {
        if (!HasComp<PowerLoaderComponent>(args.User))
            args.Cancelled = true;
    }

    private void OnGrabbableBeforeRangedInteract(Entity<PowerLoaderGrabbableComponent> ent, ref BeforeRangedInteractEvent args)
    {
        if (args.Target is not { } target)
            return;

        args.Handled = true;

        var user = new Entity<PowerLoaderComponent?>(args.User, null);
        var used = args.Used;
        var powerLoaderEv = new PowerLoaderInteractEvent(args.User, target, args.Used, GetBuckled(args.User).ToList());
        RaiseLocalEvent(used, ref powerLoaderEv);
        if (powerLoaderEv.Handled)
            return;

        var slotEv = new GetAttachmentSlotEvent(GetNetEntity(user), GetNetEntity(used));
        RaiseLocalEvent(target, slotEv);
        if (string.IsNullOrWhiteSpace(slotEv.SlotId))
            return;

        var slot = _container.EnsureContainer<ContainerSlot>(target, slotEv.SlotId);

        if (!slotEv.CanUse)
            return;

        if (!TryComp(used, out PowerLoaderAttachableComponent? attachableComponent) ||
            !_tag.HasAnyTag(target, attachableComponent.AttachableTypes))
        {
            return;
        }

        var ev = new DropshipAttachDoAfterEvent(GetNetEntity(target), GetNetEntity(used), slot.ID);
        var doAfter = new DoAfterArgs(EntityManager, user, attachableComponent.AttachDelay, ev, target, target, used)
        {
            BreakOnMove = true,
            DuplicateCondition = DuplicateConditions.SameEvent,
        };
        if (_doAfter.TryStartDoAfter(doAfter) && TryComp<PowerLoaderComponent>(args.User, out var loader))
            loader.DoAfter = ev.DoAfter;
    }

    private void OnActivePilotPreventCollide(Entity<ActivePowerLoaderPilotComponent> ent, ref PreventCollideEvent args)
    {
        args.Cancelled = true;
    }

    private void OnActivePilotStunned<T>(Entity<ActivePowerLoaderPilotComponent> ent, ref T args)
    {
        RemovePilot(ent);
    }

    private void OnActivePilotMobStateChanged(Entity<ActivePowerLoaderPilotComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Critical || args.NewMobState == MobState.Dead)
            OnActivePilotStunned(ent, ref args);
    }

    private void OnDropshipPartPowerLoaderInteract(Entity<DropshipFabricatorPrintableComponent> ent, ref PowerLoaderInteractEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp(args.Target, out DropshipFabricatorComponent? fabricator) ||
            !HasComp<DropshipFabricatorPointsComponent>(fabricator.Account))
            return;

        args.Handled = true;

        var delayMultiplier = 1f;
        if (TryComp(args.PowerLoader, out MovementRelayTargetComponent? relay))
            delayMultiplier = _skills.GetSkillDelayMultiplier(relay.Source, ent.Comp.RecycleSkill);

        var delay = ent.Comp.Delay * delayMultiplier;
        var ev = new DropshipFabricatoreRecycleDoafterEvent();
        var doAfter = new DoAfterArgs(EntityManager, args.PowerLoader, delay, ev, args.Target, args.Target, args.Used)
        {
            BreakOnMove = true,
            DuplicateCondition = DuplicateConditions.SameEvent,
        };

        if (_doAfter.TryStartDoAfter(doAfter) && TryComp<PowerLoaderComponent>(args.PowerLoader, out var loader))
            loader.DoAfter = ev.DoAfter;
    }

    private bool CanAttachPopup(
        ref Entity<PowerLoaderComponent?> user,
        Entity<DropshipWeaponPointComponent> target,
        EntityUid used,
        [NotNullWhen(true)] out ContainerSlot? slot)
    {
        slot = null;
        if (!Resolve(user, ref user.Comp, false))
            return false;

        string slotId;
        string msg;
        if (HasComp<DropshipWeaponComponent>(used))
        {
            slotId = target.Comp.WeaponContainerSlotId;
            msg = Loc.GetString("rmc-power-loader-occupied-weapon");
        }
        else if (HasComp<DropshipAmmoComponent>(used))
        {
            if (_transform.GetGrid(target.Owner) is { } grid &&
                _dropship.IsInFlight(grid))
            {
                return false;
            }

            slotId = target.Comp.AmmoContainerSlotId;
            msg = Loc.GetString("rmc-power-loader-occupied-ammo");

            if (!_container.TryGetContainer(target, target.Comp.WeaponContainerSlotId, out var weaponContainer) ||
                weaponContainer.ContainedEntities.Count == 0)
            {
                msg = Loc.GetString("rmc-power-loader-ammo-no-weapon");
                foreach (var buckled in GetBuckled(user))
                {
                    _popup.PopupClient(msg, target, buckled, PopupType.SmallCaution);
                }

                return false;
            }
        }
        else
        {
            return false;
        }

        slot = _container.EnsureContainer<ContainerSlot>(target, slotId);
        if (slot.ContainedEntity == null)
            return true;

        foreach (var buckled in GetBuckled(user))
        {
            _popup.PopupClient(msg, target, buckled, PopupType.SmallCaution);
        }

        slot = null;
        return false;
    }

    private void CanAttachPopup(ref Entity<PowerLoaderComponent?> user,
        EntityUid target,
        string container,
        EntityUid used,
        [NotNullWhen(true)] out ContainerSlot? slot)
    {
        slot = null;
        if (!Resolve(user, ref user.Comp, false))
            return;

        string slotId;
        string msg;
        if (HasComp<DropshipUtilityComponent>(used) ||
            HasComp<DropshipEngineComponent>(used))
        {
            slotId = container;
            msg = Loc.GetString("rmc-power-loader-occupied");
        }
        else
        {
            return;
        }

        slot = _container.EnsureContainer<ContainerSlot>(target, slotId);
        if (slot.ContainedEntity == null)
            return;

        foreach (var buckled in GetBuckled(user))
        {
            _popup.PopupClient(msg, target, buckled, PopupType.SmallCaution);
        }

        slot = null;
    }

    private bool CanDetachPopup(
    ref Entity<PowerLoaderComponent?> user,
    EntityUid target,
    string containerId,
    bool doPopup,
    [NotNullWhen(true)] out ContainerSlot? slot)
    {
        slot = null;
        if (!Resolve(user, ref user.Comp, false))
        {
            return false;
        }

        if (!HasFreeHands(user))
        {
            if (doPopup)
            {
                var msg = Loc.GetString("rmc-power-loader-cant-grab-full", ("mech", user.Owner));
                foreach (var buckled in GetBuckled(user))
                {
                    _popup.PopupClient(msg, target, buckled, PopupType.SmallCaution);
                }
            }

            return false;
        }

        if (_container.TryGetContainer(target, containerId, out var utilityContainer) &&
            utilityContainer.ContainedEntities.Count > 0)
        {
            slot = (ContainerSlot) utilityContainer;
        }

        if (slot == null)
        {
            if (doPopup)
            {
                foreach (var buckled in GetBuckled(user))
                {
                    var msg = Loc.GetString("rmc-power-loader-nothing-attached");
                    _popup.PopupClient(msg, user, buckled, PopupType.SmallCaution);
                }
            }

            return false;
        }

        return true;
    }

    private bool HasFreeHands(Entity<PowerLoaderComponent?> user)
    {
        return _hands.CountFreeHands(user.Owner) > 0;
    }

    private bool CanPickupPopup(
        Entity<PowerLoaderComponent> loader,
        Entity<PowerLoaderGrabbableComponent?> grabbable,
        out TimeSpan delay)
    {
        delay = TimeSpan.Zero;
        if (!Resolve(grabbable, ref grabbable.Comp, false))
            return false;

        if (!HasFreeHands((loader, loader)))
        {
            var msg = Loc.GetString("rmc-power-loader-cant-grab-full", ("mech", loader));
            foreach (var buckled in GetBuckled(loader))
            {
                _popup.PopupClient(msg, buckled, buckled, PopupType.SmallCaution);
            }
        }

        delay = grabbable.Comp.Delay;
        return true;
    }

    private IEnumerable<EntityUid> GetBuckled(EntityUid loader)
    {
        if (!TryComp(loader, out StrapComponent? strap))
            yield break;

        foreach (var entity in strap.BuckledEntities)
        {
            yield return entity;
        }
    }

    private void SyncHands(Entity<PowerLoaderComponent> loader)
    {
        if (_net.IsClient)
            return;

        var virtualContainer = _container.EnsureContainer<Container>(loader, loader.Comp.VirtualContainerId);
        foreach (var buckled in GetBuckled(loader))
        {
            foreach (var virt in virtualContainer.ContainedEntities.ToArray())
            {
                _virtualItem.DeleteInHandsMatching(buckled, virt);
                _container.Remove(virt, virtualContainer);
                QueueDel(virt);
            }
        }

        var toSpawn = new List<(EntityUid? Grabbed, EntProtoId Virtual, string? Name, HandLocation Location)>();
        foreach (var handId in _hands.EnumerateHands(loader.Owner))
        {
            if (!_hands.TryGetHand(loader.Owner, handId, out var hand))
                continue;

            if (!_hands.TryGetHeldItem(loader.Owner, handId, out var held))
            {
                var virtualSide = hand.Value.Location == HandLocation.Right
                    ? loader.Comp.VirtualRight
                    : loader.Comp.VirtualLeft;

                toSpawn.Add((null, virtualSide, null, hand.Value.Location));
                continue;
            }

            if (_powerLoaderGrabbableQuery.TryComp(held, out var grabbable))
            {
                var id = hand.Value.Location == HandLocation.Right ? grabbable.VirtualRight : grabbable.VirtualLeft;
                var name = Name(held.Value);
                toSpawn.Add((held, id, name, hand.Value.Location));
            }
        }

        foreach (var (grabbed, spawnVirtual, name, location) in toSpawn)
        {
            if (!TrySpawnInContainer(spawnVirtual, loader, loader.Comp.VirtualContainerId, out var virtualEnt))
                continue;

            var loaderVirtual = EnsureComp<PowerLoaderVirtualItemComponent>(virtualEnt.Value);
            loaderVirtual.Grabbed = grabbed;
            Dirty(virtualEnt.Value, loaderVirtual);

            if (name != null)
                _metaData.SetEntityName(virtualEnt.Value, name);

            foreach (var buckled in GetBuckled(loader))
            {
                if (_hands.EnumerateHands(buckled).TryFirstOrDefault(h => _hands.TryGetHand(buckled, h, out var h2) && h2.Value.Location == location, out var hand) &&
                    _virtualItem.TrySpawnVirtualItemInHand(virtualEnt.Value, buckled, out var virt, empty: hand))
                {
                    EnsureComp<UnremoveableComponent>(virt.Value);
                }
            }
        }
    }

    public void TrySyncHands(Entity<PowerLoaderComponent?> loader)
    {
        if (Resolve(loader, ref loader.Comp, false))
            SyncHands((loader, loader.Comp));
    }

    private void DeleteVirtuals(Entity<PowerLoaderComponent> loader, EntityUid user)
    {
        var virtualContainer = _container.EnsureContainer<Container>(loader, loader.Comp.VirtualContainerId);
        foreach (var contained in virtualContainer.ContainedEntities)
        {
            _virtualItem.DeleteInHandsMatching(user, contained);
        }
    }

    private void RemoveLoader(Entity<PowerLoaderComponent> loader)
    {
        foreach (var buckled in GetBuckled(loader))
        {
            if (TerminatingOrDeleted(buckled))
                continue;

            DeleteVirtuals(loader, buckled);
        }
    }

    private void PickUp(Entity<PowerLoaderComponent> loader, EntityUid target)
    {
        if (!CanPickupPopup(loader, target, out _))
            return;

        foreach (var buckled in GetBuckled(loader))
        {
            if (_hands.GetActiveHand(buckled) is not { } activeId ||
                !_hands.TryGetHand(buckled, activeId, out var active))
            {
                continue;
            }

            if (_hands.EnumerateHands(loader.Owner).TryFirstOrDefault(h => _hands.TryGetHand(loader.Owner, h, out var h2) && h2.Value.Location == active.Value.Location, out var loaderHand))
            {
                _hands.DoPickup(loader, loaderHand, target);
                SyncHands(loader);
                return;
            }
        }
    }

    private void SyncAppearance(Entity<DropshipWeaponPointComponent> point)
    {
        if (!_container.TryGetContainer(point, point.Comp.WeaponContainerSlotId, out var weaponContainer) ||
            weaponContainer.ContainedEntities.Count == 0)
        {
            _appearance.SetData(point, DropshipWeaponVisuals.Sprite, "");
            _appearance.SetData(point, DropshipWeaponVisuals.State, "");
            return;
        }

        var hasAmmo = false;
        var hasRounds = false;
        if (_container.TryGetContainer(point, point.Comp.AmmoContainerSlotId, out var ammoContainer))
        {
            foreach (var contained in ammoContainer.ContainedEntities)
            {
                if (TryComp(contained, out DropshipAmmoComponent? ammo))
                {
                    hasAmmo = true;

                    // TODO RMC14 partial reloads? or hide it anyways if below the threshold
                    if (ammo.Rounds >= ammo.RoundsPerShot)
                        hasRounds = true;
                }
            }
        }

        foreach (var contained in weaponContainer.ContainedEntities)
        {
            if (!TryComp(contained, out DropshipWeaponComponent? weapon))
                continue;

            SpriteSpecifier.Rsi? rsi;
            if (hasAmmo && hasRounds)
                rsi = weapon.AmmoAttachedSprite;
            else if (hasAmmo)
                rsi = weapon.AmmoEmptyAttachedSprite;
            else
                rsi = weapon.WeaponAttachedSprite;

            if (rsi == null)
                continue;

            _appearance.SetData(point, DropshipWeaponVisuals.Sprite, rsi.RsiPath.ToString());
            _appearance.SetData(point, DropshipWeaponVisuals.State, rsi.RsiState);
        }
    }

    private void SyncAppearance(EntityUid point, string container)
    {
        if (!_container.TryGetContainer(point, container, out var utilityContainer) ||
            utilityContainer.ContainedEntities.Count == 0)
        {
            _appearance.SetData(point, DropshipUtilityVisuals.Sprite, "");
            _appearance.SetData(point, DropshipUtilityVisuals.State, "");
            return;
        }

        foreach (var contained in utilityContainer.ContainedEntities)
        {
            if (!TryComp(contained, out DropshipAttachedSpriteComponent? utility))
                continue;

            if (utility.Sprite is not { } rsi)
                continue;

            _appearance.SetData(point, DropshipUtilityVisuals.Sprite, rsi.RsiPath.ToString());
            _appearance.SetData(point, DropshipUtilityVisuals.State, rsi.RsiState);
            return;
        }
    }

    private void RemovePilot(Entity<ActivePowerLoaderPilotComponent> active)
    {
        _buckle.Unbuckle(active.Owner, null);
        RemCompDeferred<ActivePowerLoaderPilotComponent>(active);
    }

    private bool TryGetPointContainer(DropshipDoAfterEvent args, out Entity<PowerLoaderComponent> user, out EntityUid container, out EntityUid contained, [NotNullWhen(true)] out BaseContainer? slot)
    {
        user = default;
        container = default;
        contained = default;
        slot = null;
        if (args.Cancelled ||
            args.Handled ||
            args.Target == null)
        {
            return false;
        }

        if (!TryComp(args.User, out PowerLoaderComponent? powerLoaderComp))
            return false;

        args.Handled = true;
        user = new Entity<PowerLoaderComponent>(args.User, powerLoaderComp);
        container = GetEntity(args.Container);
        contained = GetEntity(args.Contained);
        slot = _container.GetContainer(container, args.Slot);
        return true;
    }

    private void InsertPoint(Entity<PowerLoaderComponent> user, EntityUid contained, BaseContainer slot)
    {
        if (slot.ContainedEntities.Count > 0)
            return;

        _container.Insert(contained, slot);
        SyncHands((user, user.Comp));
    }

    private void StartPointDetach<T>(Entity<T> ent, ContainerSlot container, Entity<PowerLoaderComponent> user, EntityUid target) where T : IComponent?
    {
        if (!TryComp(container.ContainedEntity, out PowerLoaderDetachableComponent? detachableComponent))
            return;

        var contained = container.ContainedEntity.Value;
        var ev = new DropshipDetachDoAfterEvent(GetNetEntity(ent), GetNetEntity(contained), container.ID);
        var doAfter = new DoAfterArgs(EntityManager, user, detachableComponent.DetachDelay, ev, target, target)
        {
            BreakOnMove = true,
            DuplicateCondition = DuplicateConditions.SameEvent,
        };

        if (_doAfter.TryStartDoAfter(doAfter))
            user.Comp.DoAfter = ev.DoAfter;
    }

    private void TryStartPointDetach<T>(Entity<T> ent, string container, ref ActivateInWorldEvent args) where T : IComponent?
    {
        if (!TryComp(args.User, out PowerLoaderComponent? loader))
            return;

        args.Handled = true;

        var user = new Entity<PowerLoaderComponent?>(args.User, loader);
        var target = args.Target;
        if (!CanDetachPopup(ref user, ent, container, true, out var slot) ||
            slot.ContainedEntity == null)
        {
            return;
        }

        StartPointDetach(ent, slot, (user, loader), target);
    }

    private void DetachPoint(ref DropshipDetachDoAfterEvent args)
    {
        if (!TryGetPointContainer(args, out var user, out _, out var contained, out var slot))
            return;

        if (TryComp(contained, out DropshipUtilityComponent? utilityComp))
            utilityComp.AttachmentPoint = null;

        _container.Remove(contained, slot);

        PickUp((user, user.Comp), contained);
        SyncHands((user, user.Comp));
    }

    private void TryGetSlot(EntityUid ent, string container, ref GetAttachmentSlotEvent args)
    {
        var user = new Entity<PowerLoaderComponent?>(GetEntity(args.User), null);
        if (!TryGetEntity(args.Used, out var used))
            return;

        ContainerSlot? slot;
        if (args.BeingAttached)
            CanAttachPopup(ref user, ent, container, used.Value, out slot);
        else
            CanDetachPopup(ref user, ent, container, true, out slot);

        if (slot == null)
            return;

        args.SlotId = slot.ID;
    }

    public override void Update(float frameTime)
    {
        var pilots = EntityQueryEnumerator<ActivePowerLoaderPilotComponent>();
        while (pilots.MoveNext(out var uid, out var active))
        {
            if (!TryComp(uid, out BuckleComponent? buckle) ||
                !HasComp<PowerLoaderComponent>(buckle.BuckledTo))
            {
                RemovePilot((uid, active));
            }
        }
    }
}
