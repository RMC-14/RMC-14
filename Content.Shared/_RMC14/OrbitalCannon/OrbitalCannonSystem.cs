using System.Linq;
using Content.Shared._RMC14.Animations;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Atmos;
using Content.Shared._RMC14.CameraShake;
using Content.Shared._RMC14.Chat;
using Content.Shared._RMC14.Explosion;
using Content.Shared._RMC14.GameStates;
using Content.Shared._RMC14.Intel;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Marines.Announce;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Mortar;
using Content.Shared._RMC14.PowerLoader;
using Content.Shared._RMC14.Rules;
using Content.Shared.Administration.Logs;
using Content.Shared.Chat;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Ghost;
using Content.Shared.Maps;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.UserInterface;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.OrbitalCannon;

public sealed class OrbitalCannonSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly SharedRMCAnimationSystem _animation = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly AreaSystem _area = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly IntelSystem _intel = default!;
    [Dependency] private readonly SharedMarineAnnounceSystem _marineAnnounce = default!;
    [Dependency] private readonly SharedMortarSystem _mortar = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly PowerLoaderSystem _powerLoader = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RMCCameraShakeSystem _rmcCameraShake = default!;
    [Dependency] private readonly SharedCMChatSystem _rmcChat = default!;
    [Dependency] private readonly SharedRMCFlammableSystem _rmcFlammable = default!;
    [Dependency] private readonly SharedRMCExplosionSystem _rmcExplosion = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly RMCPlanetSystem _rmcPlanet = default!;
    [Dependency] private readonly SharedRMCPvsSystem _rmcPvs = default!;
    [Dependency] private readonly TagSystem _tags = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private static readonly EntProtoId OrbitalTargetMarker = "RMCLaserDropshipTarget";

    public override void Initialize()
    {
        SubscribeLocalEvent<OrbitalCannonComponent, MapInitEvent>(OnOrbitalCannonMapInit);
        SubscribeLocalEvent<OrbitalCannonComponent, ComponentShutdown>(OnOrbitalCannonShutdown);

        SubscribeLocalEvent<OrbitalCannonTrayComponent, PowerLoaderGrabEvent>(OnTrayPowerLoaderGrab);
        SubscribeLocalEvent<OrbitalCannonTrayComponent, EntInsertedIntoContainerMessage>(OnTrayContainerInserted);
        SubscribeLocalEvent<OrbitalCannonTrayComponent, EntRemovedFromContainerMessage>(OnTrayContainerRemoved);

        SubscribeLocalEvent<OrbitalCannonWarheadComponent, PowerLoaderInteractEvent>(OnWarheadPowerLoaderInteract);
        SubscribeLocalEvent<OrbitalCannonWarheadComponent, OrbitalBombardmentFireEvent>(OnWarheadOrbitalBombardmentFire);

        SubscribeLocalEvent<OrbitalCannonFuelComponent, PowerLoaderInteractEvent>(OnFuelPowerLoaderInteract);

        SubscribeLocalEvent<OrbitalCannonComputerComponent, BeforeActivatableUIOpenEvent>(OnComputerBeforeActivatableUIOpen);

        Subs.BuiEvents<OrbitalCannonComputerComponent>(OrbitalCannonComputerUI.Key,
            subs =>
            {
                subs.Event<OrbitalCannonComputerLoadBuiMsg>(OnComputerLoad);
                subs.Event<OrbitalCannonComputerUnloadBuiMsg>(OnComputerUnload);
                subs.Event<OrbitalCannonComputerChamberBuiMsg>(OnComputerChamber);
            });
    }

    private void OnOrbitalCannonMapInit(Entity<OrbitalCannonComponent> ent, ref MapInitEvent args)
    {
        var possibleFuels = ent.Comp.PossibleFuelRequirements.ToList();
        foreach (var warhead in ent.Comp.WarheadTypes)
        {
            if (possibleFuels.Count <= 0)
                possibleFuels = ent.Comp.PossibleFuelRequirements.ToList();

            if (possibleFuels.Count <= 0)
            {
                Log.Error($"No possible fuel choice found for {warhead}");
                return;
            }

            var fuel = _random.PickAndTake(possibleFuels);
            ent.Comp.FuelRequirements.Add(new WarheadFuelRequirement(warhead, fuel));
        }

        // Spawn the tray at offset position and link it to this cannon
        if (_net.IsServer && ent.Comp.TrayPrototype != null)
        {
            var trayCoords = _transform.GetMoverCoordinates(ent).Offset(ent.Comp.TraySpawnOffset);
            var trayId = SpawnAttachedTo(ent.Comp.TrayPrototype.Value, trayCoords);
            if (TryComp(trayId, out OrbitalCannonTrayComponent? tray))
            {
                ent.Comp.LinkedTray = trayId;
                tray.LinkedCannon = ent;
                Dirty(trayId, tray);
            }
        }

        Dirty(ent);
    }

    private void OnOrbitalCannonShutdown(Entity<OrbitalCannonComponent> ent, ref ComponentShutdown args)
    {
        if (_net.IsServer && ent.Comp.LinkedTray is { } trayId)
            QueueDel(trayId);
    }

    private void OnTrayPowerLoaderGrab(Entity<OrbitalCannonTrayComponent> ent, ref PowerLoaderGrabEvent args)
    {
        if (args.Handled)
            return;

        // Can't grab from tray if cannon is loaded/chambered
        if (ent.Comp.LinkedCannon is { } cannonId &&
            TryComp(cannonId, out OrbitalCannonComponent? cannon) &&
            cannon.Status != OrbitalCannonStatus.Unloaded)
        {
            return;
        }

        if (_container.TryGetContainer(ent, ent.Comp.FuelContainer, out var fuel) &&
            fuel.ContainedEntities.Count > 0)
        {
            args.ToGrab = fuel.ContainedEntities[^1];
            args.Handled = true;
        }
        else if (_container.TryGetContainer(ent, ent.Comp.WarheadContainer, out var warhead) &&
                 warhead.ContainedEntities.Count > 0)
        {
            args.ToGrab = warhead.ContainedEntities[^1];
            args.Handled = true;
        }

        if (args.Handled && _net.IsServer)
        {
            if (ent.Comp.LinkedCannon is { } linkedCannonId && TryComp(linkedCannonId, out OrbitalCannonComponent? linkedCannon))
                _audio.PlayPvs(linkedCannon.UnloadItemSound, args.Target);
        }
    }

    private void OnTrayContainerInserted(Entity<OrbitalCannonTrayComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.WarheadContainer && args.Container.ID != ent.Comp.FuelContainer)
            return;

        if (_net.IsServer)
            UpdateTrayVisuals(ent);
    }

    private void OnTrayContainerRemoved(Entity<OrbitalCannonTrayComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.WarheadContainer && args.Container.ID != ent.Comp.FuelContainer)
            return;

        if (_net.IsServer)
            UpdateTrayVisuals(ent);
    }

    private void UpdateTrayVisuals(Entity<OrbitalCannonTrayComponent> tray)
    {
        EntProtoId<OrbitalCannonWarheadComponent>? warheadType = null;
        var fuelAmount = 0;

        if (_container.TryGetContainer(tray, tray.Comp.WarheadContainer, out var warheadContainer) &&
            warheadContainer.ContainedEntities.Count > 0)
        {
            var warheadEntity = warheadContainer.ContainedEntities[0];
            if (HasComp<OrbitalCannonWarheadComponent>(warheadEntity))
            {
                var protoId = MetaData(warheadEntity).EntityPrototype?.ID;
                if (protoId != null)
                    warheadType = protoId;
            }
        }

        if (_container.TryGetContainer(tray, tray.Comp.FuelContainer, out var fuelContainer))
            fuelAmount = fuelContainer.ContainedEntities.Count;

        tray.Comp.WarheadType = warheadType;
        tray.Comp.FuelAmount = fuelAmount;
        Dirty(tray);

        _appearance.SetData(tray, OrbitalCannonTrayVisuals.Warhead, warheadType?.Id ?? "None");
        _appearance.SetData(tray, OrbitalCannonTrayVisuals.Fuel, fuelAmount);
    }

    private void OnWarheadPowerLoaderInteract(Entity<OrbitalCannonWarheadComponent> ent, ref PowerLoaderInteractEvent args)
    {
        if (!TryComp(args.Target, out OrbitalCannonTrayComponent? tray))
            return;

        args.Handled = true;
        if (tray.LinkedCannon is { } cannonId &&
            TryComp(cannonId, out OrbitalCannonComponent? cannon) &&
            cannon.Status != OrbitalCannonStatus.Unloaded)
        {
            foreach (var buckled in args.Buckled)
            {
                _popup.PopupClient("The tray is already loaded into the cannon!", args.Target, buckled, PopupType.MediumCaution);
            }

            return;
        }

        var container = _container.EnsureContainer<ContainerSlot>(args.Target, tray.WarheadContainer);
        if (container.ContainedEntity != null)
        {
            foreach (var buckled in args.Buckled)
            {
                _popup.PopupClient("There is already a warhead loaded!", args.Target, buckled, PopupType.MediumCaution);
            }

            return;
        }

        if (!_container.Insert(args.Used, container))
        {
            foreach (var buckled in args.Buckled)
            {
                _popup.PopupClient($"You can't insert {Name(args.Used)} into the {Name(args.Target)}!", args.Target, buckled, PopupType.MediumCaution);
            }
        }

        _popup.PopupClient($"You load {Name(args.Used)} into the {Name(args.Target)}!", args.Target, args.Target, PopupType.Medium);
        _powerLoader.TrySyncHands(args.PowerLoader);

        if (_net.IsServer)
        {
            if (tray.LinkedCannon is { } linkedCannonId && TryComp(linkedCannonId, out OrbitalCannonComponent? linkedCannon))
                _audio.PlayPvs(linkedCannon.LoadItemSound, args.Target);
        }
    }

    private void OnWarheadOrbitalBombardmentFire(Entity<OrbitalCannonWarheadComponent> ent, ref OrbitalBombardmentFireEvent args)
    {
        var coordinates = _transform.ToCoordinates(args.Coordinates);

        // check for indestructible walls at impact location and try to find alternative
        if (TileHasIndestructibleWalls(coordinates))
        {
            var found = false;

            for (var x = -1; x <= 1; x++)
            {
                for (var y = -1; y <= 1; y++)
                {
                    // Skip the center position (original impact location)
                    if (x == 0 && y == 0)
                        continue;

                    var offset = new Vector2i(x, y);
                    var testMapCoordinates = args.Coordinates.Offset(offset);
                    if (!_rmcMap.TryGetTileDef(testMapCoordinates, out var tile) ||
                        tile.ID == ContentTileDefinition.SpaceID)
                        continue;

                    var testCoordinates = _transform.ToCoordinates(testMapCoordinates);
                    if (!_area.CanOrbitalBombard(testCoordinates, out var roofed))
                        continue;

                    if (!TileHasIndestructibleWalls(testCoordinates))
                    {
                        coordinates = testCoordinates;
                        Log.Info($"Orbital bombardment impact redirected due to indestructible wall at impact site");
                        found = true;
                        break;
                    }
                }

                if (found)
                    break;
            }

            if (!found)
            {
                // No valid alternative found, warhead fizzles out like in BYOND
                Log.Info($"Orbital bombardment impact blocked by indestructible walls, no valid alternative found");
                return;
            }
        }

        Spawn(ent.Comp.Explosion, coordinates);
    }

    private void OnFuelPowerLoaderInteract(Entity<OrbitalCannonFuelComponent> ent, ref PowerLoaderInteractEvent args)
    {
        if (!TryComp(args.Target, out OrbitalCannonTrayComponent? tray))
            return;

        args.Handled = true;
        if (tray.LinkedCannon is { } cannonId &&
            TryComp(cannonId, out OrbitalCannonComponent? cannon) &&
            cannon.Status != OrbitalCannonStatus.Unloaded)
        {
            foreach (var buckled in args.Buckled)
            {
                _popup.PopupClient("The tray is already loaded into the cannon!", buckled, PopupType.MediumCaution);
            }

            return;
        }

        if (!_container.TryGetContainer(args.Target, tray.WarheadContainer, out var warheadContainer) ||
            warheadContainer.ContainedEntities.Count == 0)
        {
            foreach (var buckled in args.Buckled)
            {
                _popup.PopupClient($"A warhead must be placed in the {Name(args.Target)} first.", args.Target, buckled, PopupType.MediumCaution);
            }

            return;
        }

        var fuelContainer = _container.EnsureContainer<Container>(args.Target, tray.FuelContainer);
        if (fuelContainer.ContainedEntities.Count >= tray.MaxFuel)
        {
            foreach (var buckled in args.Buckled)
            {
                _popup.PopupClient($"The {Name(args.Target)} can't accept more solid fuel!", args.Target, buckled, PopupType.MediumCaution);
            }

            return;
        }

        if (!_container.Insert(args.Used, fuelContainer))
        {
            foreach (var buckled in args.Buckled)
            {
                _popup.PopupClient($"You can't insert {Name(args.Used)} into the {Name(args.Target)}!", args.Target, buckled, PopupType.MediumCaution);
            }

            return;
        }

        _popup.PopupClient($"You load {Name(args.Used)} into the {Name(args.Target)}!", args.Target, args.Target, PopupType.Medium);
        _powerLoader.TrySyncHands(args.PowerLoader);

        if (_net.IsServer)
        {
            if (tray.LinkedCannon is { } linkedCannonId && TryComp(linkedCannonId, out OrbitalCannonComponent? linkedCannon))
                _audio.PlayPvs(linkedCannon.LoadItemSound, args.Target);
        }
    }

    private void OnComputerBeforeActivatableUIOpen(Entity<OrbitalCannonComputerComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        if (!TryGetClosestCannon(ent, out var cannon))
            return;

        ent.Comp.FuelRequirements = cannon.Comp.FuelRequirements;
        ent.Comp.Status = cannon.Comp.Status;

        if (cannon.Comp.LinkedTray is { } trayId && TryComp(trayId, out OrbitalCannonTrayComponent? tray))
        {
            ent.Comp.Warhead = _container.TryGetContainer(trayId, tray.WarheadContainer, out var warheadContainer) &&
                               warheadContainer.ContainedEntities.Count > 0
                ? Name(warheadContainer.ContainedEntities[0])
                : null;
            ent.Comp.Fuel = _container.TryGetContainer(trayId, tray.FuelContainer, out var fuelContainer)
                ? fuelContainer.ContainedEntities.Count
                : 0;
        }
        else
        {
            ent.Comp.Warhead = null;
            ent.Comp.Fuel = 0;
        }

        Dirty(ent);
    }

    private void OnComputerLoad(Entity<OrbitalCannonComputerComponent> ent, ref OrbitalCannonComputerLoadBuiMsg args)
    {
        if (!TryGetClosestCannon(ent, out var cannon))
            return;

        if (cannon.Comp.Status != OrbitalCannonStatus.Unloaded)
            return;

        if (cannon.Comp.LinkedTray is not { } trayId || !TryComp(trayId, out OrbitalCannonTrayComponent? tray))
            return;

        if (!TrayHasWarhead((trayId, tray)) || TrayGetFuel((trayId, tray)) <= 0)
            return;

        var time = _timing.CurTime;
        if (time < cannon.Comp.LastToggledAt + cannon.Comp.ToggleCooldown)
            return;

        var cannonChamber = _container.EnsureContainer<ContainerSlot>(cannon, cannon.Comp.CannonChamberContainer);
        if (!_container.Insert(trayId, cannonChamber))
            return;

        cannon.Comp.LastToggledAt = time;
        cannon.Comp.Status = OrbitalCannonStatus.Loaded;
        Dirty(cannon);

        ent.Comp.Status = cannon.Comp.Status;
        Dirty(ent);

        if (_net.IsServer)
            _audio.PlayPvs(cannon.Comp.LoadSound, cannon);

        _animation.TryFlick(cannon.Owner, cannon.Comp.LoadingAnimation, cannon.Comp.LoadedState, cannon.Comp.BaseLayerKey);
        CannonStatusChanged(cannon);
    }

    private void OnComputerUnload(Entity<OrbitalCannonComputerComponent> ent, ref OrbitalCannonComputerUnloadBuiMsg args)
    {
        if (!TryGetClosestCannon(ent, out var cannon))
            return;

        if (cannon.Comp.Status != OrbitalCannonStatus.Loaded)
            return;

        var time = _timing.CurTime;
        if (time < cannon.Comp.LastToggledAt + cannon.Comp.ToggleCooldown)
            return;

        if (!_container.TryGetContainer(cannon, cannon.Comp.CannonChamberContainer, out var cannonChamber) ||
            cannonChamber.ContainedEntities.Count == 0)
        {
            return;
        }

        cannon.Comp.LastToggledAt = time;
        cannon.Comp.Status = OrbitalCannonStatus.Unloaded;
        cannon.Comp.UnloadingTrayAt = time;
        Dirty(cannon);

        ent.Comp.Status = cannon.Comp.Status;
        Dirty(ent);

        if (_net.IsServer)
            _audio.PlayPvs(cannon.Comp.UnloadSound, cannon);

        _animation.TryFlick(cannon.Owner, cannon.Comp.UnloadingAnimation, cannon.Comp.UnloadedState, cannon.Comp.BaseLayerKey);
        CannonStatusChanged(cannon);
    }

    private void OnComputerChamber(Entity<OrbitalCannonComputerComponent> ent, ref OrbitalCannonComputerChamberBuiMsg args)
    {
        if (!TryGetClosestCannon(ent, out var cannon))
            return;

        if (cannon.Comp.Status != OrbitalCannonStatus.Loaded)
            return;

        if (!CannonHasWarhead(cannon) || CannonGetFuel(cannon) <= 0)
            return;

        var time = _timing.CurTime;
        if (time < cannon.Comp.LastToggledAt + cannon.Comp.ToggleCooldown)
            return;

        cannon.Comp.LastToggledAt = time;
        cannon.Comp.Status = OrbitalCannonStatus.Chambered;
        Dirty(cannon);

        ent.Comp.Status = cannon.Comp.Status;
        Dirty(ent);

        if (_net.IsServer)
            _audio.PlayPvs(cannon.Comp.ChamberSound, cannon);

        _animation.TryFlick(cannon.Owner, cannon.Comp.ChamberingAnimation, cannon.Comp.ChamberedState, cannon.Comp.BaseLayerKey);
        CannonStatusChanged(cannon);
    }

    public bool TryGetClosestCannon(EntityUid to, out Entity<OrbitalCannonComponent> cannon)
    {
        cannon = default;
        if (!TryComp(to, out TransformComponent? transform))
            return false;

        var last = float.MaxValue;
        var query = EntityQueryEnumerator<OrbitalCannonComponent, TransformComponent>();
        while (query.MoveNext(out var cannonId, out var cannonComp, out var cannonTransform))
        {
            if (transform.Coordinates.TryDistance(EntityManager,
                    _transform,
                    cannonTransform.Coordinates,
                    out var distance) &&
                distance < last)
            {
                last = distance;
                cannon = (cannonId, cannonComp);
            }
        }

        return cannon != default;
    }

    private bool CannonHasWarhead(Entity<OrbitalCannonComponent> cannon, out EntityUid warhead)
    {
        warhead = default;
        if (cannon.Comp.LinkedTray is not { } trayId || !TryComp(trayId, out OrbitalCannonTrayComponent? tray))
            return false;

        if (_container.TryGetContainer(trayId, tray.WarheadContainer, out var container) &&
            container.ContainedEntities.Count > 0 &&
            !EntityManager.IsQueuedForDeletion(container.ContainedEntities[0]))
        {
            warhead = container.ContainedEntities[0];
            return true;
        }

        return false;
    }

    private bool CannonHasWarhead(Entity<OrbitalCannonComponent> cannon)
    {
        return CannonHasWarhead(cannon, out _);
    }

    private int CannonGetFuel(Entity<OrbitalCannonComponent> cannon)
    {
        if (cannon.Comp.LinkedTray is not { } trayId || !TryComp(trayId, out OrbitalCannonTrayComponent? tray))
            return 0;

        if (!_container.TryGetContainer(trayId, tray.FuelContainer, out var container))
            return 0;

        return container.ContainedEntities.Count;
    }

    private bool TrayHasWarhead(Entity<OrbitalCannonTrayComponent> tray, out EntityUid warhead)
    {
        warhead = default;
        if (_container.TryGetContainer(tray, tray.Comp.WarheadContainer, out var container) &&
            container.ContainedEntities.Count > 0 &&
            !EntityManager.IsQueuedForDeletion(container.ContainedEntities[0]))
        {
            warhead = container.ContainedEntities[0];
            return true;
        }

        return false;
    }

    private bool TrayHasWarhead(Entity<OrbitalCannonTrayComponent> tray)
    {
        return TrayHasWarhead(tray, out _);
    }

    private int TrayGetFuel(Entity<OrbitalCannonTrayComponent> tray)
    {
        if (!_container.TryGetContainer(tray, tray.Comp.FuelContainer, out var container))
            return 0;

        return container.ContainedEntities.Count;
    }

    private void CannonStatusChanged(Entity<OrbitalCannonComponent> cannon)
    {
        if (cannon.Comp.LinkedTray is { } trayId && TryComp(trayId, out OrbitalCannonTrayComponent? tray))
            UpdateTrayVisuals((trayId, tray));
        var ev = new OrbitalCannonChangedEvent(cannon, CannonHasWarhead(cannon), CannonGetFuel(cannon));
        RaiseLocalEvent(cannon, ref ev, true);
    }

    private bool TileHasIndestructibleWalls(EntityCoordinates coordinates)
    {
        var anchoredEntities = _rmcMap.GetAnchoredEntitiesEnumerator(coordinates);

        while (anchoredEntities.MoveNext(out var entity))
        {
            // This part is shitty because there might be a wall that just... doesn't exactly go with this logic. Hope it works.
            if (HasComp<TagComponent>(entity) &&
                _tags.HasTag(entity, "Wall") &&
                !HasComp<DamageableComponent>(entity))
            {
                return true;
            }
        }

        return false;
    }

    public bool Fire(Entity<OrbitalCannonComponent> cannon, Vector2i fireCoordinates, EntityUid user, EntityUid squad)
    {
        if (_net.IsClient)
            return false;

        if (cannon.Comp.Status != OrbitalCannonStatus.Chambered)
            return false;

        var time = _timing.CurTime;
        if (cannon.Comp.LastFireAt != null &&
            time < cannon.Comp.LastFireAt + cannon.Comp.FireCooldown)
        {
            return false;
        }

        if (cannon.Comp.LinkedTray is not { } trayId || !TryComp(trayId, out OrbitalCannonTrayComponent? tray))
        {
            _popup.PopupCursor("The orbital cannon has no linked tray.", user, PopupType.LargeCaution);
            return false;
        }

        if (!_container.TryGetContainer(trayId, tray.WarheadContainer, out var warheadContainer) ||
            warheadContainer.ContainedEntities.Count == 0)
        {
            _popup.PopupCursor("The orbital cannon has no ammo chambered.", user, PopupType.LargeCaution);
            return false;
        }

        if (!_rmcPlanet.TryPlanetToCoordinates(fireCoordinates, out var planetCoordinates))
        {
            _popup.PopupCursor("The target zone appears to be out of bounds. Please check coordinates.", user, PopupType.LargeCaution);
            return false;
        }

        if (!_rmcMap.TryGetTileDef(planetCoordinates, out var tile) ||
            tile.ID == ContentTileDefinition.SpaceID)
        {
            _popup.PopupCursor("The target zone appears to be out of bounds. Please check coordinates.", user, PopupType.LargeCaution);
            return false;
        }

        if (!_area.CanOrbitalBombard(_transform.ToCoordinates(planetCoordinates), out var roofed))
        {
            if (roofed)
            {
                _popup.PopupCursor("The target zone has strong biological protection. The orbital strike cannot reach here.", user, PopupType.LargeCaution);
                return false;
            }

            _popup.PopupCursor("The target zone is deep underground. The orbital strike cannot reach here.", user, PopupType.LargeCaution);
            return false;
        }

        _popup.PopupCursor("Orbital bombardment request accepted. Orbital cannons are now calibrating.", PopupType.Large);

        var warhead = warheadContainer.ContainedEntities[0];
        var misfuel = 0;
        if (_container.TryGetContainer(trayId, tray.FuelContainer, out var fuelContainer))
        {
            var fuel = fuelContainer.ContainedEntities.Count;
            var warheadProto = Prototype(warhead)?.ID;
            if (cannon.Comp.FuelRequirements.TryFirstOrNull(f => f.Warhead.Id == warheadProto, out var requirement))
                misfuel = Math.Abs(fuel - requirement.Value.Fuel);
        }

        var offset = misfuel + 1;
        var offsetX = offset * _random.Next(-3, 3);
        var offsetY = offset * _random.Next(-3, 3);
        var adjustedCoords = fireCoordinates + new Vector2i(offsetX, offsetY);

        var firing = EnsureComp<OrbitalCannonFiringComponent>(cannon);
        firing.Coordinates = adjustedCoords;
        firing.WarheadName = Name(warhead);
        firing.Squad = squad;
        firing.StartedAt = time;

        if (TryComp(warhead, out OrbitalCannonWarheadComponent? warheadComp))
        {
            firing.FirstWarningRange = warheadComp.FirstWarningRange;
            firing.SecondWarningRange = warheadComp.SecondWarningRange;
            firing.ThirdWarningRange = warheadComp.ThirdWarningRange;

            // Award intel points for specific warhead types
            if (warheadComp.IntelPointsAwarded > 0 && _net.IsServer)
            {
                _intel.AddPoints(warheadComp.IntelPointsAwarded);
            }
        }

        Dirty(cannon, firing);

        _popup.PopupCursor("Orbital bombardment launched!", user);

        var logMessage = $"{ToPrettyString(user)} launched orbital bombardment at {fireCoordinates} for squad {ToPrettyString(squad)}, misfuel: {misfuel}, final coords: {adjustedCoords}";
        _adminLog.Add(LogType.RMCOrbitalBombardment, $"{logMessage}");

        var ev = new OrbitalCannonLaunchEvent(cannon.Comp.FireCooldown + firing.ImpactDelay);
        RaiseLocalEvent(ref ev);
        return true;
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var cannonQuery = EntityQueryEnumerator<OrbitalCannonComponent>();
        while (cannonQuery.MoveNext(out var uid, out var cannon))
        {
            if (cannon.UnloadingTrayAt == null)
                continue;

            if (time < cannon.UnloadingTrayAt + cannon.UnloadingTrayDelay)
                continue;

            cannon.UnloadingTrayAt = null;
            Dirty(uid, cannon);

            if (!_container.TryGetContainer(uid, cannon.CannonChamberContainer, out var cannonChamber) ||
                cannonChamber.ContainedEntities.Count == 0)
            {
                continue;
            }

            var trayId = cannonChamber.ContainedEntities[0];
            var trayCoords = _transform.GetMoverCoordinates(uid).Offset(cannon.TraySpawnOffset);
            _container.Remove(trayId, cannonChamber);
            _transform.SetCoordinates(trayId, trayCoords);

            if (TryComp(trayId, out OrbitalCannonTrayComponent? tray))
                UpdateTrayVisuals((trayId, tray));
        }

        var firingQuery = EntityQueryEnumerator<OrbitalCannonFiringComponent, OrbitalCannonComponent>();
        while (firingQuery.MoveNext(out var uid, out var firing, out var cannon))
        {
            if (!_rmcPlanet.TryPlanetToCoordinates(firing.Coordinates, out var planetCoordinates))
            {
                RemCompDeferred<OrbitalCannonFiringComponent>(uid);
                continue;
            }

            if (!firing.Alerted && time > firing.StartedAt + firing.AlertDelay)
            {
                firing.Alerted = true;
                Dirty(uid, firing);

                var groundFilter = Filter
                    .BroadcastMap(planetCoordinates.MapId)
                    .RemoveWhereAttachedEntity(e => !HasComp<SquadMemberComponent>(e) && !HasComp<GhostComponent>(e));

                _audio.PlayGlobal(cannon.GroundAlertSound, groundFilter, true);

                var msg = "[font size=16][color=red]Orbital bombardment launch command detected![/color][/font]";
                _rmcChat.ChatMessageToMany(msg, msg, groundFilter, ChatChannel.Radio);

                if (_area.TryGetArea(planetCoordinates, out _, out var areaProto))
                {
                    msg = $"[color=red]Launch command informs {firing.WarheadName}. Estimated impact area: {areaProto.Name}[/color]";
                    _rmcChat.ChatMessageToMany(msg, msg, groundFilter, ChatChannel.Radio);
                }
            }

            if (!firing.BegunFire && time > firing.StartedAt + firing.BeginFireDelay)
            {
                firing.BegunFire = true;
                Dirty(uid, firing);

                var map = _transform.GetMapId(uid);
                var sameMap = Filter.BroadcastMap(map);
                _rmcCameraShake.ShakeCamera(sameMap, 10, 1);
                _audio.PlayPvs(cannon.FireSound, uid);
                _animation.TryFlick(uid, cannon.FiringAnimation, cannon.ChamberedState, cannon.BaseLayerKey);

                var msg = "[color=red]The deck of the UNS Almayer shudders as the orbital cannons open fire on the colony.[/color]";
                _rmcChat.ChatMessageToMany(msg, msg, sameMap, ChatChannel.Radio);

                _marineAnnounce.AnnounceSquad("WARNING! Ballistic trans-atmospheric launch detected! Get outside of Danger Close!", firing.Squad);
            }

            if (!firing.Fired && time > firing.StartedAt + firing.FireDelay)
            {
                firing.Fired = true;
                Dirty(uid, firing);

                var planetEntCoordinates = _transform.ToCoordinates(planetCoordinates);
                _audio.PlayPvs(cannon.TravelSound, planetEntCoordinates, AudioParams.Default.WithMaxDistance(75));

                _mortar.PopupWarning(planetCoordinates, firing.FirstWarningRange, "rmc-ob-warning-one", "rmc-ob-warning-above-one", true);
            }

            if (!firing.WarnedOne && time > firing.StartedAt + firing.WarnOneDelay)
            {
                firing.WarnedOne = true;
                Dirty(uid, firing);
                _mortar.PopupWarning(planetCoordinates, firing.SecondWarningRange, "rmc-ob-warning-two", "rmc-ob-warning-above-two", true);
            }

            if (!firing.WarnedTwo && time > firing.StartedAt + firing.WarnTwoDelay)
            {
                firing.WarnedTwo = true;
                Dirty(uid, firing);
                _mortar.PopupWarning(planetCoordinates, firing.ThirdWarningRange, "rmc-ob-warning-three", "rmc-ob-warning-above-three", true);
            }

            if (!firing.AegisBoomed && time > firing.StartedAt + firing.AegisBoomDelay)
            {
                firing.AegisBoomed = true;
                Dirty(uid, firing);

                if (CannonHasWarhead((uid, cannon), out var foundWarhead) &&
                    TryComp(foundWarhead, out OrbitalCannonWarheadComponent? foundWarheadComp) &&
                    foundWarheadComp.IsAegis)
                {
                    var planetEntCoordinates = _transform.ToCoordinates(planetCoordinates);
                    var sound = _audio.PlayPvs(cannon.AegisBoomSound, planetEntCoordinates, AudioParams.Default.WithMaxDistance(300));
                    if (sound != null)
                        _rmcPvs.AddGlobalOverride(sound.Value.Entity);
                }
            }

            if (!firing.Impacted && time > firing.StartedAt + firing.ImpactDelay)
            {
                firing.Impacted = true;
                cannon.Status = OrbitalCannonStatus.Unloaded;
                cannon.LastFireAt = time;
                Dirty(uid, cannon);

                var cannonEnt = new Entity<OrbitalCannonComponent>(uid, cannon);
                var fuel = CannonGetFuel(cannonEnt);
                if (CannonHasWarhead(cannonEnt, out var warhead))
                {
                    var ev = new OrbitalBombardmentFireEvent(cannonEnt, warhead, fuel, planetCoordinates);
                    RaiseLocalEvent(warhead, ref ev);
                }

                if (_container.TryGetContainer(uid, cannon.CannonChamberContainer, out var cannonChamber) &&
                    cannonChamber.ContainedEntities.Count > 0)
                {
                    var trayId = cannonChamber.ContainedEntities[0];
                    if (TryComp(trayId, out OrbitalCannonTrayComponent? tray))
                    {
                        if (_container.TryGetContainer(trayId, tray.FuelContainer, out var fuelContainer))
                            _container.CleanContainer(fuelContainer);

                        if (_container.TryGetContainer(trayId, tray.WarheadContainer, out var warheadContainer))
                            _container.CleanContainer(warheadContainer);
                    }
                }

                cannon.UnloadingTrayAt = time;
                _animation.TryFlick(uid, cannon.UnloadingAnimation, cannon.UnloadedState, cannon.BaseLayerKey);
                Dirty(uid, cannon);
                CannonStatusChanged(cannonEnt);
                RemCompDeferred<OrbitalCannonFiringComponent>(uid);
            }
        }

        var explosionQuery = EntityQueryEnumerator<OrbitalCannonExplosionComponent>();
        while (explosionQuery.MoveNext(out var uid, out var explosion))
        {
            if (!explosion.Laser)
            {
                explosion.Laser = true;
                Spawn(OrbitalTargetMarker, _transform.GetMapCoordinates(uid));
            }

            if (explosion.Current == default && explosion.LastAt == default)
            {
                explosion.LastAt = time;
                Dirty(uid, explosion);
            }

            if (explosion.Current >= explosion.Steps.Count)
            {
                QueueDel(uid);
                continue;
            }

            var step = explosion.Steps[explosion.Current];
            if (time >= explosion.LastAt + step.Delay)
            {
                if (step.Times <= 1)
                {
                    explosion.Current++;
                    Dirty(uid, explosion);
                }
                else
                {
                    if (time < explosion.LastStepAt + step.DelayPer)
                        continue;

                    explosion.Step++;
                    explosion.LastStepAt = time;
                    if (explosion.Step >= step.Times)
                    {
                        explosion.Current++;
                        explosion.Step = 0;
                        explosion.LastStepAt = default;
                        Dirty(uid, explosion);
                    }
                }

                // TODO RMC14 cluster laser pointers
                for (var i = 0; i < step.TimesPer; i++)
                {
                    var mapCoordinates = _transform.GetMapCoordinates(uid);
                    var coordinates = _transform.GetMoverCoordinates(uid);
                    if (step.Spread > 0)
                    {
                        var spread = _random.NextVector2(-step.Spread, step.Spread);
                        mapCoordinates = mapCoordinates.Offset(spread);
                        coordinates = coordinates.Offset(spread);
                    }

                    if (step.CheckProtectionPer && !_area.CanOrbitalBombard(coordinates, out _))
                        continue;

                    if (step.ExplosionEffect != null)
                    {
                        var effect = Spawn(step.ExplosionEffect.Value, mapCoordinates);
                        _rmcExplosion.TryDoEffect(effect);
                    }

                    if (step.Type is { } type)
                        _rmcExplosion.QueueExplosion(mapCoordinates, type, step.Total, step.Slope, step.Max, uid, canCreateVacuum: false);

                    if (step.Fire is { } fire && step.FireRange > 0)
                        _rmcFlammable.SpawnFireDiamond(fire, coordinates, step.FireRange);
                }
            }
        }
    }
}
