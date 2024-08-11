using System.Linq;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.Buckle.Components;
using Content.Shared.Coordinates;
using Content.Shared.DoAfter;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Item;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using static Content.Shared.Interaction.SharedInteractionSystem;

namespace Content.Shared._RMC14.PowerLoader;

public sealed class PowerLoaderSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly SharedMoverController _mover = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
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

        SubscribeLocalEvent<PowerLoaderGrabbableComponent, PickupAttemptEvent>(OnGrabbablePickupAttempt);
        SubscribeLocalEvent<PowerLoaderGrabbableComponent, AfterInteractEvent>(OnGrabbableAfterInteract);
    }

    private void OnPowerLoaderMapInit(Entity<PowerLoaderComponent> ent, ref MapInitEvent args)
    {
        _container.EnsureContainer<Container>(ent, ent.Comp.ContainerId);
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

        _mover.SetRelay(buckle, ent);
        _interaction.SetRelay(buckle, ent, relay);
        _movementSpeed.RefreshMovementSpeedModifiers(ent);
        SyncHands(ent);
    }

    private void OnUnstrapped(Entity<PowerLoaderComponent> ent, ref UnstrappedEvent args)
    {
        var buckle = args.Buckle;
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
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnGrabDoAfter(Entity<PowerLoaderComponent> ent, ref PowerLoaderGrabDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target)
            return;

        if (!CanPickupPopup(ent, target, out _))
            return;

        var container = _container.EnsureContainer<Container>(ent, ent.Comp.ContainerId);
        _container.Insert(target, container);
        SyncHands(ent);
    }

    private void OnGetUsedEntity(Entity<PowerLoaderComponent> ent, ref GetUsedEntityEvent args)
    {
        var container = _container.EnsureContainer<Container>(ent, ent.Comp.ContainerId);
        foreach (var buckled in GetBuckled(ent))
        {
            if (!TryComp(buckled, out HandsComponent? hands))
                continue;

            if (!_hands.TryGetActiveHand((buckled, hands), out var hand))
                continue;

            var sorted = hands.SortedHands;
            var index = sorted.IndexOf(hand.Name);
            if (index == -1)
                continue;

            if (index >= container.ContainedEntities.Count)
                continue;

            args.Used = container.ContainedEntities[index];
            break;
        }
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

        var container = _container.EnsureContainer<Container>(user, loader.ContainerId);
        if (!container.ContainedEntities.Contains(ent))
            return;

        var source = ent.Owner.ToCoordinates();
        var coords = _transform.GetMoverCoordinates(args.ClickLocation);
        if (!source.TryDistance(EntityManager, coords, out var distance))
            return;

        if (distance < 0.5f)
        {
            var msg = Loc.GetString("rmc-power-loader-too-close");
            foreach (var buckled in GetBuckled((user, loader)))
            {
                _popup.PopupClient(msg, ent, buckled, PopupType.SmallCaution);
            }

            return;
        }

        if (distance > InteractionRange)
        {
            var msg = Loc.GetString("rmc-power-loader-too-far");
            foreach (var buckled in GetBuckled((user, loader)))
            {
                _popup.PopupClient(msg, ent, buckled, PopupType.SmallCaution);
            }

            return;
        }

        args.Handled = true;
        var used = args.Used;
        _container.Remove(used, container, destination: coords, localRotation: Angle.Zero);
        SyncHands((user, loader));
    }

    private bool CanPickupPopup(
        Entity<PowerLoaderComponent> loader,
        Entity<PowerLoaderGrabbableComponent?> grabbable,
        out TimeSpan delay)
    {
        delay = default;
        if (!Resolve(grabbable, ref grabbable.Comp, false))
            return false;

        var container = _container.EnsureContainer<Container>(loader, loader.Comp.ContainerId);
        if (container.ContainedEntities.Count >= loader.Comp.ContainerLimit)
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

    private IEnumerable<EntityUid> GetBuckled(Entity<PowerLoaderComponent> loader)
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

        var container = _container.EnsureContainer<Container>(loader, loader.Comp.ContainerId);
        var toSpawn = new List<EntProtoId<VirtualItemComponent>>();
        foreach (var contained in container.ContainedEntities)
        {
            if (_powerLoaderGrabbableQuery.TryComp(contained, out var grabbable))
                toSpawn.Add(virtualContainer.ContainedEntities.Count == 0 ? grabbable.VirtualRight : grabbable.VirtualLeft);
        }

        if (toSpawn.Count == 0)
            toSpawn.Add(loader.Comp.VirtualRight);

        if (toSpawn.Count == 1)
            toSpawn.Add(loader.Comp.VirtualLeft);

        foreach (var spawn in toSpawn)
        {
            if (!TrySpawnInContainer(spawn, loader, loader.Comp.VirtualContainerId, out var virtualEnt))
                continue;

            foreach (var buckled in GetBuckled(loader))
            {
                if (_virtualItem.TrySpawnVirtualItemInHand(virtualEnt.Value, buckled, out var virt))
                    EnsureComp<UnremoveableComponent>(virt.Value);
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
}
