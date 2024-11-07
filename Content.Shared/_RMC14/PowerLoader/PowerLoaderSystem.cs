using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.Dropship.Weapon;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Coordinates;
using Content.Shared.Coordinates.Helpers;
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
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using static Content.Shared.Interaction.SharedInteractionSystem;
using static Content.Shared.Physics.CollisionGroup;

namespace Content.Shared._RMC14.PowerLoader;

public sealed class PowerLoaderSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly SharedMoverController _mover = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedRMCMapSystem _rmcMap = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedVirtualItemSystem _virtualItem = default!;

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
        SubscribeLocalEvent<PowerLoaderComponent, UserActivateInWorldEvent>(OnUserActivateInWorld);
        SubscribeLocalEvent<PowerLoaderComponent, PowerLoaderGrabDoAfterEvent>(OnGrabDoAfter);
        SubscribeLocalEvent<PowerLoaderComponent, GetUsedEntityEvent>(OnGetUsedEntity);

        SubscribeLocalEvent<DropshipWeaponPointComponent, ActivateInWorldEvent>(OnPointActivateInWorld);

        SubscribeLocalEvent<PowerLoaderGrabbableComponent, PickupAttemptEvent>(OnGrabbablePickupAttempt);
        SubscribeLocalEvent<PowerLoaderGrabbableComponent, AfterInteractEvent>(OnGrabbableAfterInteract);
        SubscribeLocalEvent<PowerLoaderGrabbableComponent, BeforeRangedInteractEvent>(OnGrabbableBeforeRangedInteract);
        SubscribeLocalEvent<PowerLoaderGrabbableComponent, DropshipAttachDoAfterEvent>(OnGrabbableDropshipAttach);
        SubscribeLocalEvent<PowerLoaderGrabbableComponent, DropshipDetachDoAfterEvent>(OnGrabbableDropshipDetach);
        SubscribeLocalEvent<PowerLoaderGrabbableComponent, CombatModeShouldHandInteractEvent>(OnGrababbleShouldInteract);

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

        if (_hands.EnumerateHands(buckle).Count(h => h.Container?.ContainedEntity == null) < 2)
        {
            if (args.Popup)
                _popup.PopupClient(Loc.GetString("rmc-power-loader-hands-occupied", ("mech", ent)), buckle, args.User);

            args.Cancelled = true;
            return;
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
    }

    private void OnUserActivateInWorld(Entity<PowerLoaderComponent> ent, ref UserActivateInWorldEvent args)
    {
        if (!CanPickupPopup(ent, args.Target, out var delay))
            return;

        var ev = new PowerLoaderGrabDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, ent, delay, ev, ent, args.Target)
        {
            BreakOnMove = true,
            DuplicateCondition = DuplicateConditions.SameEvent,
        };

        _doAfter.TryStartDoAfter(doAfter);
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

            if (!_hands.TryGetActiveHand((buckled, hands), out var hand) ||
                !TryComp(hand.HeldEntity, out VirtualItemComponent? virtualItem) ||
                !TryComp(virtualItem.BlockingEntity, out PowerLoaderVirtualItemComponent? item))
            {
                continue;
            }

            foreach (var contained in _hands.EnumerateHeld(ent))
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

    private void OnPointActivateInWorld(Entity<DropshipWeaponPointComponent> ent, ref ActivateInWorldEvent args)
    {
        if (!TryComp(args.User, out PowerLoaderComponent? loader))
            return;

        args.Handled = true;

        var user = new Entity<PowerLoaderComponent?>(args.User, loader);
        var target = args.Target;
        if (!CanDetachPopup(ref user, args.Target, out var delay, out var slot) ||
            slot.ContainedEntity is not { } contained)
        {
            return;
        }

        var ev = new DropshipDetachDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, args.User, delay, ev, contained, target)
        {
            BreakOnMove = true,
            DuplicateCondition = DuplicateConditions.SameEvent,
        };

        _doAfter.TryStartDoAfter(doAfter);
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

    private void OnGrabbableBeforeRangedInteract(Entity<PowerLoaderGrabbableComponent> ent, ref BeforeRangedInteractEvent args)
    {
        if (args.Target is not { } target)
            return;

        args.Handled = true;

        var user = new Entity<PowerLoaderComponent?>(args.User, null);
        var used = args.Used;
        if (!CanAttachPopup(ref user, target, used, out var delay, out _))
            return;

        var ev = new DropshipAttachDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, user, delay, ev, ent, target, used)
        {
            BreakOnMove = true,
            DuplicateCondition = DuplicateConditions.SameEvent,
        };
        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnGrabbableDropshipAttach(Entity<PowerLoaderGrabbableComponent> ent, ref DropshipAttachDoAfterEvent args)
    {
        if (args.Cancelled ||
            args.Handled ||
            args.Target is not { } target ||
            args.Used is not { } used)
        {
            return;
        }

        var user = new Entity<PowerLoaderComponent?>(args.User, null);
        if (!CanAttachPopup(ref user, target, used, out _, out var slot))
            return;

        _container.Insert(used, slot);

        if (user.Comp != null)
            SyncHands((user, user.Comp));

        SyncAppearance(target);
    }

    private void OnGrabbableDropshipDetach(Entity<PowerLoaderGrabbableComponent> ent, ref DropshipDetachDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target)
            return;

        args.Handled = true;

        var user = new Entity<PowerLoaderComponent?>(args.User, null);
        if (!CanDetachPopup(ref user, target, out _, out var slot) ||
            slot.ContainedEntity is not { } contained)
        {
            return;
        }

        _container.Remove(contained, slot);

        // TODO RMC14 partial ammo transfers between boxes, only delete if 0
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
        else if (user.Comp != null)
        {
            PickUp((user, user.Comp), contained);
            SyncHands((user, user.Comp));
        }

        SyncAppearance(target);
    }

    private void OnGrababbleShouldInteract(Entity<PowerLoaderGrabbableComponent> ent, ref CombatModeShouldHandInteractEvent args)
    {
        if (!HasComp<PowerLoaderComponent>(args.User))
            args.Cancelled = true;
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

    private bool CanAttachPopup(
        ref Entity<PowerLoaderComponent?> user,
        EntityUid target,
        EntityUid used,
        out TimeSpan delay,
        [NotNullWhen(true)] out ContainerSlot? slot)
    {
        delay = default;
        slot = default;
        if (!Resolve(user, ref user.Comp, false) ||
            !TryComp(target, out DropshipWeaponPointComponent? point))
        {
            return false;
        }

        string slotId;
        string msg;
        if (TryComp(used, out DropshipWeaponComponent? weapon))
        {
            slotId = point.WeaponContainerSlotId;
            delay = weapon.AttachDelay;
            msg = Loc.GetString("rmc-power-loader-occupied-weapon");
        }
        else if (TryComp(used, out DropshipAmmoComponent? ammo))
        {
            slotId = point.AmmoContainerSlotId;
            delay = ammo.AttachDelay;
            msg = Loc.GetString("rmc-power-loader-occupied-ammo");

            if (!_container.TryGetContainer(target, point.WeaponContainerSlotId, out var weaponContainer) ||
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

        slot = default;
        return false;
    }

    private bool CanDetachPopup(
        ref Entity<PowerLoaderComponent?> user,
        EntityUid target,
        out TimeSpan delay,
        [NotNullWhen(true)] out ContainerSlot? slot)
    {
        delay = default;
        slot = default;
        if (!Resolve(user, ref user.Comp, false) ||
            !TryComp(target, out DropshipWeaponPointComponent? point))
        {
            return false;
        }

        if (!HasFreeHands(user))
        {
            var msg = Loc.GetString("rmc-power-loader-cant-grab-full", ("mech", user.Owner));
            foreach (var buckled in GetBuckled(user))
            {
                _popup.PopupClient(msg, target, buckled, PopupType.SmallCaution);
            }

            return false;
        }

        if (_container.TryGetContainer(target, point.AmmoContainerSlotId, out var ammoContainer) &&
            ammoContainer.ContainedEntities.Count > 0)
        {
            slot = (ContainerSlot) ammoContainer;
            delay = CompOrNull<DropshipAmmoComponent>(slot.ContainedEntity)?.AttachDelay ?? TimeSpan.Zero;
        }
        else if (_container.TryGetContainer(target, point.WeaponContainerSlotId, out var weaponContainer) &&
                 weaponContainer.ContainedEntities.Count > 0)
        {
            slot = (ContainerSlot) weaponContainer;
            delay = CompOrNull<DropshipWeaponComponent>(slot.ContainedEntity)?.AttachDelay ?? TimeSpan.Zero;
        }

        if (slot == null)
        {
            foreach (var buckled in GetBuckled(user))
            {
                var msg = Loc.GetString("rmc-power-loader-nothing-attached");
                _popup.PopupClient(msg, user, buckled, PopupType.SmallCaution);
            }

            return false;
        }

        return true;
    }

    private bool HasFreeHands(Entity<PowerLoaderComponent?> user)
    {
        return _hands.EnumerateHands(user).Any(h => h.HeldEntity == null);
    }

    private bool CanPickupPopup(
        Entity<PowerLoaderComponent> loader,
        Entity<PowerLoaderGrabbableComponent?> grabbable,
        out TimeSpan delay)
    {
        delay = default;
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
        foreach (var hand in _hands.EnumerateHands(loader))
        {
            if (hand.HeldEntity is not { } held)
            {
                var virtualSide = hand.Location == HandLocation.Right
                    ? loader.Comp.VirtualRight
                    : loader.Comp.VirtualLeft;

                toSpawn.Add((null, virtualSide, null, hand.Location));
                continue;
            }

            if (_powerLoaderGrabbableQuery.TryComp(held, out var grabbable))
            {
                var id = hand.Location == HandLocation.Right ? grabbable.VirtualRight : grabbable.VirtualLeft;
                var name = Name(held);
                toSpawn.Add((held, id, name, hand.Location));
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
                if (_hands.EnumerateHands(buckled).TryFirstOrDefault(h => h.Location == location, out var hand) &&
                    _virtualItem.TrySpawnVirtualItemInHand(virtualEnt.Value, buckled, out var virt, empty: hand))
                {
                    EnsureComp<UnremoveableComponent>(virt.Value);
                }
            }
        }
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
            if (_hands.GetActiveHand(buckled) is not { } active)
                continue;

            if (_hands.EnumerateHands(loader).TryFirstOrDefault(h => h.Location == active.Location, out var loaderHand))
            {
                _hands.DoPickup(loader, loaderHand, target);
                SyncHands(loader);
                return;
            }
        }
    }

    private void SyncAppearance(Entity<DropshipWeaponPointComponent?> point)
    {
        if (!Resolve(point, ref point.Comp, false))
            return;

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

    private void RemovePilot(Entity<ActivePowerLoaderPilotComponent> active)
    {
        _buckle.Unbuckle(active.Owner, null);
        RemCompDeferred<ActivePowerLoaderPilotComponent>(active);
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
