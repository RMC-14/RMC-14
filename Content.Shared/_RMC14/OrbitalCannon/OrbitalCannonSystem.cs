﻿using System.Linq;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Atmos;
using Content.Shared._RMC14.CameraShake;
using Content.Shared._RMC14.Chat;
using Content.Shared._RMC14.Explosion;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Marines.Announce;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Mortar;
using Content.Shared._RMC14.PowerLoader;
using Content.Shared._RMC14.Rules;
using Content.Shared.Administration.Logs;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Ghost;
using Content.Shared.Maps;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
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
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly AreaSystem _area = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
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
    [Dependency] private readonly SharedRMCMapSystem _rmcMap = default!;
    [Dependency] private readonly RMCPlanetSystem _rmcPlanet = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private static readonly EntProtoId OrbitalTargetMarker = "RMCLaserDropshipTarget";

    public override void Initialize()
    {
        SubscribeLocalEvent<OrbitalCannonComponent, MapInitEvent>(OnOrbitalCannonMapInit);
        SubscribeLocalEvent<OrbitalCannonComponent, PowerLoaderGrabEvent>(OnOrbitalCannonPowerLoaderGrab);

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

        Dirty(ent);
        _appearance.SetData(ent, OrbitalCannonVisuals.Base, ent.Comp.Status);
    }

    private void OnOrbitalCannonPowerLoaderGrab(Entity<OrbitalCannonComponent> ent, ref PowerLoaderGrabEvent args)
    {
        if (args.Handled)
            return;

        if (_container.TryGetContainer(ent, ent.Comp.FuelContainer, out var fuel) &&
            fuel.ContainedEntities.Count > 0)
        {
            args.ToGrab = fuel.ContainedEntities[^1];
            args.Handled = true;
        }

        if (_container.TryGetContainer(ent, ent.Comp.WarheadContainer, out var warhead) &&
            warhead.ContainedEntities.Count > 0)
        {
            args.ToGrab = warhead.ContainedEntities[^1];
            args.Handled = true;
        }

        if (args.Handled && _net.IsServer)
            _audio.PlayPvs(ent.Comp.UnloadItemSound, args.Target);
    }

    private void OnWarheadPowerLoaderInteract(Entity<OrbitalCannonWarheadComponent> ent, ref PowerLoaderInteractEvent args)
    {
        if (!TryComp(args.Target, out OrbitalCannonComponent? cannon))
            return;

        args.Handled = true;
        var container = _container.EnsureContainer<ContainerSlot>(args.Target, cannon.WarheadContainer);
        if (container.ContainedEntity != null)
        {
            foreach (var buckled in args.Buckled)
            {
                _popup.PopupClient("There is already a warhead loaded!", args.Target, buckled, PopupType.MediumCaution);
            }

            return;
        }

        if (cannon.Status != OrbitalCannonStatus.Unloaded)
        {
            foreach (var buckled in args.Buckled)
            {
                _popup.PopupClient("The cannon isn't unloaded!", args.Target, buckled, PopupType.MediumCaution);
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
            _audio.PlayPvs(cannon.LoadItemSound, args.Target);
    }

    private void OnWarheadOrbitalBombardmentFire(Entity<OrbitalCannonWarheadComponent> ent, ref OrbitalBombardmentFireEvent args)
    {
        Spawn(ent.Comp.Explosion, args.Coordinates);
    }

    private void OnFuelPowerLoaderInteract(Entity<OrbitalCannonFuelComponent> ent, ref PowerLoaderInteractEvent args)
    {
        if (!TryComp(args.Target, out OrbitalCannonComponent? cannon))
            return;

        args.Handled = true;
        if (!_container.TryGetContainer(args.Target, cannon.WarheadContainer, out var warheadContainer) ||
            warheadContainer.ContainedEntities.Count == 0)
        {
            foreach (var buckled in args.Buckled)
            {
                _popup.PopupClient($"A warhead must be placed in the {Name(args.Target)} first.", args.Target, buckled, PopupType.MediumCaution);
            }

            return;
        }

        if (cannon.Status != OrbitalCannonStatus.Unloaded)
        {
            foreach (var buckled in args.Buckled)
            {
                _popup.PopupClient($"The {Name(args.Target)} isn't unloaded!", args.Target, buckled, PopupType.MediumCaution);
            }

            return;
        }

        var fuelContainer = _container.EnsureContainer<Container>(args.Target, cannon.FuelContainer);
        if (fuelContainer.ContainedEntities.Count >= cannon.MaxFuel)
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
            _audio.PlayPvs(cannon.LoadItemSound, args.Target);
    }

    private void OnComputerBeforeActivatableUIOpen(Entity<OrbitalCannonComputerComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        if (!TryGetClosestCannon(ent, out var cannon))
            return;

        ent.Comp.Warhead = _container.TryGetContainer(cannon, cannon.Comp.WarheadContainer, out var warheadContainer) &&
                           warheadContainer.ContainedEntities.Count > 0
            ? Name(warheadContainer.ContainedEntities[0])
            : null;
        ent.Comp.Fuel = _container.TryGetContainer(cannon, cannon.Comp.FuelContainer, out var fuelContainer)
            ? fuelContainer.ContainedEntities.Count
            : 0;
        ent.Comp.FuelRequirements = cannon.Comp.FuelRequirements;
        ent.Comp.Status = cannon.Comp.Status;

        Dirty(ent);
    }

    private void OnComputerLoad(Entity<OrbitalCannonComputerComponent> ent, ref OrbitalCannonComputerLoadBuiMsg args)
    {
        if (!TryGetClosestCannon(ent, out var cannon))
            return;

        if (cannon.Comp.Status != OrbitalCannonStatus.Unloaded)
            return;

        if (!CannonHasWarhead(cannon) || CannonGetFuel(cannon) <= 0)
            return;

        var time = _timing.CurTime;
        if (time < cannon.Comp.LastToggledAt + cannon.Comp.ToggleCooldown)
            return;

        cannon.Comp.LastToggledAt = time;
        cannon.Comp.Status = OrbitalCannonStatus.Loaded;
        Dirty(cannon);

        ent.Comp.Status = cannon.Comp.Status;
        Dirty(ent);

        if (_net.IsServer)
            _audio.PlayPvs(cannon.Comp.LoadSound, cannon);

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

        cannon.Comp.LastToggledAt = time;
        cannon.Comp.Status = OrbitalCannonStatus.Unloaded;
        Dirty(cannon);

        ent.Comp.Status = cannon.Comp.Status;
        Dirty(ent);

        if (_net.IsServer)
            _audio.PlayPvs(cannon.Comp.UnloadSound, cannon);

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
        if (_container.TryGetContainer(cannon, cannon.Comp.WarheadContainer, out var container) &&
            container.ContainedEntities.Count > 0 &&
            !EntityManager.IsQueuedForDeletion(container.ContainedEntities[0]))
        {
            warhead = container.ContainedEntities[0];
            return true;
        }

        warhead = default;
        return false;
    }

    private bool CannonHasWarhead(Entity<OrbitalCannonComponent> cannon)
    {
        return CannonHasWarhead(cannon, out _);
    }

    private int CannonGetFuel(Entity<OrbitalCannonComponent> cannon)
    {
        if (!_container.TryGetContainer(cannon, cannon.Comp.FuelContainer, out var container))
            return 0;

        return container.ContainedEntities.Count;
    }

    private void CannonStatusChanged(Entity<OrbitalCannonComponent> cannon)
    {
        _appearance.SetData(cannon, OrbitalCannonVisuals.Base, cannon.Comp.Status);
        var ev = new OrbitalCannonChangedEvent(cannon, CannonHasWarhead(cannon), CannonGetFuel(cannon));
        RaiseLocalEvent(cannon, ref ev, true);
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

        if (!_container.TryGetContainer(cannon, cannon.Comp.WarheadContainer, out var warheadContainer) ||
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
        if (_container.TryGetContainer(cannon, cannon.Comp.FuelContainer, out var fuelContainer))
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
        Dirty(cannon, firing);

        _popup.PopupCursor("Orbital bombardment launched!", user);
        _adminLog.Add(LogType.RMCOrbitalBombardment, $"{ToPrettyString(user)} launched orbital bombardment at {fireCoordinates} for squad {ToPrettyString(squad)}, misfuel: {misfuel}, final coords: {adjustedCoords}");
        return true;
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
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

                if (_area.TryGetArea(planetCoordinates, out _, out var areaProto, out _))
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

                var msg = "[color=red]The deck of the UNS Almayer shudders as the orbital cannons open fire on the colony.[/color]";
                _rmcChat.ChatMessageToMany(msg, msg, sameMap, ChatChannel.Radio);

                _marineAnnounce.AnnounceSquad("WARNING! Ballistic trans-atmospheric launch detected! Get outside of Danger Close!", firing.Squad);
            }

            if (!firing.Fired && time > firing.StartedAt + firing.FireDelay)
            {
                firing.Fired = true;
                Dirty(uid, firing);

                _audio.PlayPvs(cannon.FireSound, uid);

                var planetEntCoordinates = _transform.ToCoordinates(planetCoordinates);
                _audio.PlayPvs(cannon.TravelSound, planetEntCoordinates, AudioParams.Default.WithMaxDistance(75));

                _mortar.PopupWarning(planetCoordinates, 30, "rmc-ob-warning-one", "rmc-ob-warning-above-one", true);
            }

            if (!firing.WarnedOne && time > firing.StartedAt + firing.WarnOneDelay)
            {
                firing.WarnedOne = true;
                Dirty(uid, firing);
                _mortar.PopupWarning(planetCoordinates, 25, "rmc-ob-warning-two", "rmc-ob-warning-above-two", true);
            }

            if (!firing.WarnedTwo && time > firing.StartedAt + firing.WarnTwoDelay)
            {
                firing.WarnedTwo = true;
                Dirty(uid, firing);
                _mortar.PopupWarning(planetCoordinates, 15, "rmc-ob-warning-three", "rmc-ob-warning-above-three", true);
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

                CannonStatusChanged(cannonEnt);
                RemCompDeferred<OrbitalCannonFiringComponent>(uid);

                if (_container.TryGetContainer(uid, cannon.FuelContainer, out var fuelContainer))
                    _container.CleanContainer(fuelContainer);

                if (_container.TryGetContainer(uid, cannon.WarheadContainer, out var warheadContainer))
                    _container.CleanContainer(warheadContainer);
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

            if (explosion.Current == default && explosion.LastStepAt == default)
            {
                explosion.LastStepAt = time;
                Dirty(uid, explosion);
            }

            if (explosion.Current >= explosion.Steps.Count)
            {
                QueueDel(uid);
                continue;
            }

            var step = explosion.Steps[explosion.Current];
            if (time >= explosion.LastStepAt + step.Delay)
            {
                explosion.Current++;
                Dirty(uid, explosion);

                if (step.Type != default)
                {
                    var coordinates = _transform.GetMapCoordinates(uid);
                    _rmcExplosion.QueueExplosion(coordinates, step.Type, step.Total, step.Slope, step.Max, uid);
                }

                if (step.Fire is { } fire && step.FireRange > 0)
                {
                    var coordinates = _transform.GetMoverCoordinates(uid);
                    _rmcFlammable.SpawnFireDiamond(fire, coordinates, step.FireRange);
                }
            }
        }
    }
}
