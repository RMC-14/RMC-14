using System.Runtime.InteropServices;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Rangefinder;
using Content.Shared._RMC14.Rules;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Content.Shared.IgnitionSource;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Light.Components;
using Content.Shared.Popups;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Systems;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using static Content.Shared._RMC14.Dropship.Weapon.DropshipTerminalWeaponsComponent;
using static Content.Shared._RMC14.Dropship.Weapon.DropshipTerminalWeaponsScreen;

namespace Content.Shared._RMC14.Dropship.Weapon;

public abstract class SharedDropshipWeaponSystem : EntitySystem
{
    [Dependency] private readonly AreaSystem _area = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedDropshipSystem _dropship = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly SquadSystem _squad = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    private static readonly EntProtoId DropshipTargetMarker = "RMCLaserDropshipTarget";

    private bool _casDebug;
    private readonly HashSet<Entity<DamageableComponent>> _damageables = new();
    private int _nextId = 1;

    public override void Initialize()
    {
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);

        SubscribeLocalEvent<FlareSignalComponent, IgnitionEvent>(OnFlareSignalIgnition);
        SubscribeLocalEvent<FlareSignalComponent, GettingPickedUpAttemptEvent>(OnFlareSignalPickupAttempt);
        SubscribeLocalEvent<FlareSignalComponent, ExaminedEvent>(OnFlareSignalExamined);
        SubscribeLocalEvent<FlareSignalComponent, DroppedEvent>(OnFlareSignalDropped);
        SubscribeLocalEvent<FlareSignalComponent, ThrownEvent>(OnFlareSignalThrown);
        SubscribeLocalEvent<FlareSignalComponent, StopThrowEvent>(OnFlareSignalStopThrow);
        SubscribeLocalEvent<FlareSignalComponent, ContainerGettingInsertedAttemptEvent>(OnFlareSignalContainerGettingInsertedAttempt);

        SubscribeLocalEvent<DropshipTerminalWeaponsComponent, MapInitEvent>(OnTerminalMapInit);
        SubscribeLocalEvent<DropshipTerminalWeaponsComponent, BoundUIOpenedEvent>(OnTerminalBUIOpened);
        SubscribeLocalEvent<DropshipTerminalWeaponsComponent, BoundUIClosedEvent>(OnTerminalBUIClosed);

        SubscribeLocalEvent<DropshipTargetComponent, MapInitEvent>(OnDropshipTargetMapInit);
        SubscribeLocalEvent<DropshipTargetComponent, ComponentRemove>(OnDropshipTargetRemove);
        SubscribeLocalEvent<DropshipTargetComponent, EntityTerminatingEvent>(OnDropshipTargetRemove);

        SubscribeLocalEvent<DropshipAmmoComponent, ExaminedEvent>(OnAmmoExamined);

        Subs.BuiEvents<DropshipTerminalWeaponsComponent>(DropshipTerminalWeaponsUi.Key,
            subs =>
            {
                subs.Event<DropshipTerminalWeaponsChangeScreenMsg>(OnWeaponsChangeScreenMsg);
                subs.Event<DropshipTerminalWeaponsChooseWeaponMsg>(OnWeaponsChooseWeaponMsg);
                subs.Event<DropshipTerminalWeaponsFireMsg>(OnWeaponsFireMsg);
                subs.Event<DropshipTerminalWeaponsNightVisionMsg>(OnWeaponsNightVisionMsg);
                subs.Event<DropshipTerminalWeaponsExitMsg>(OnWeaponsExitMsg);
                subs.Event<DropshipTerminalWeaponsCancelMsg>(OnWeaponsCancelMsg);
                subs.Event<DropshipTerminalWeaponsAdjustOffsetMsg>(OnWeaponsAdjustOffset);
                subs.Event<DropshipTerminalWeaponsResetOffsetMsg>(OnWeaponsResetOffset);
                subs.Event<DropshipTerminalWeaponsTargetsPreviousMsg>(OnWeaponsTargetsPrevious);
                subs.Event<DropshipTerminalWeaponsTargetsNextMsg>(OnWeaponsTargetsNext);
                subs.Event<DropshipTerminalWeaponsTargetsSelectMsg>(OnWeaponsTargetsSelect);
            });

        Subs.CVar(_config, RMCCVars.RMCDropshipCASDebug, v => _casDebug = v, true);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _nextId = 1;
    }

    private void OnFlareSignalIgnition(Entity<FlareSignalComponent> ent, ref IgnitionEvent args)
    {
        if (args.Ignite)
            return;

        _physics.SetBodyType(ent, BodyType.Dynamic);
        RemCompDeferred<DropshipTargetComponent>(ent);
    }

    private void OnFlareSignalPickupAttempt(Entity<FlareSignalComponent> ent, ref GettingPickedUpAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (IsFlareLit(ent))
            args.Cancel();
    }

    private void OnFlareSignalExamined(Entity<FlareSignalComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(FlareSignalComponent)))
        {
            if (TryComp(ent, out ExpendableLightComponent? expendable) && expendable.CurrentState != ExpendableLightState.Dead)
            {
                args.PushMarkup(Loc.GetString("rmc-laser-designator-signal-flare-examine"));
            }
        }
    }

    private void OnFlareSignalDropped(Entity<FlareSignalComponent> ent, ref DroppedEvent args)
    {
        StartTrackingActiveFlare(ent, args.User);
    }

    private void OnFlareSignalThrown(Entity<FlareSignalComponent> ent, ref ThrownEvent args)
    {
        StartTrackingActiveFlare(ent, args.User);
    }

    private void OnFlareSignalStopThrow(Entity<FlareSignalComponent> ent, ref StopThrowEvent args)
    {
        if (HasComp<DropshipTargetComponent>(ent))
            _physics.SetBodyType(ent, BodyType.Static);
    }

    private void OnFlareSignalContainerGettingInsertedAttempt(Entity<FlareSignalComponent> ent, ref ContainerGettingInsertedAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (IsFlareLit(ent))
            args.Cancel();
    }

    private void OnTerminalMapInit(Entity<DropshipTerminalWeaponsComponent> ent, ref MapInitEvent args)
    {
        var targets = new List<TargetEnt>();
        var targetsQuery = EntityQueryEnumerator<DropshipTargetComponent>();
        while (targetsQuery.MoveNext(out var uid, out var target))
        {
            targets.Add(new TargetEnt(GetNetEntity(uid), target.Abbreviation));
        }

        targets.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
        ent.Comp.Targets = targets;
        Dirty(ent);
    }

    private void OnTerminalBUIOpened(Entity<DropshipTerminalWeaponsComponent> ent, ref BoundUIOpenedEvent args)
    {
        AddPvs(ent, args.Actor);
    }

    private void OnTerminalBUIClosed(Entity<DropshipTerminalWeaponsComponent> ent, ref BoundUIClosedEvent args)
    {
        RemovePvs(ent, args.Actor);
    }

    private void OnDropshipTargetMapInit(Entity<DropshipTargetComponent> ent, ref MapInitEvent args)
    {
        var eye = EnsureComp<EyeComponent>(ent);
        _eye.SetDrawFov(ent, false, eye);

        var netEnt = GetNetEntity(ent);
        var terminals = EntityQueryEnumerator<DropshipTerminalWeaponsComponent>();
        while (terminals.MoveNext(out var uid, out var terminal))
        {
            terminal.Targets.Add(new TargetEnt(netEnt, ent.Comp.Abbreviation));
            Dirty(uid, terminal);
        }
    }

    private void OnDropshipTargetRemove<T>(Entity<DropshipTargetComponent> ent, ref T args)
    {
        var netUid = GetNetEntity(ent);
        var terminals = EntityQueryEnumerator<DropshipTerminalWeaponsComponent>();
        while (terminals.MoveNext(out var uid, out var terminal))
        {
            if (terminal.Target == ent)
            {
                RemovePvsActors((uid, terminal));
                terminal.Target = null;
            }

            var span = CollectionsMarshal.AsSpan(terminal.Targets);
            for (var i = 0; i < span.Length; i++)
            {
                ref var target = ref span[i];
                if (target.Id != netUid)
                    continue;

                terminal.Targets.RemoveAt(i);
                break;
            }

            Dirty(uid, terminal);
        }
    }

    private void OnAmmoExamined(Entity<DropshipAmmoComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(DropshipAmmoComponent)))
        {
            args.PushText(Loc.GetString("rmc-dropship-ammo-examine", ("rounds", ent.Comp.Rounds)));
        }
    }

    private void OnWeaponsChangeScreenMsg(Entity<DropshipTerminalWeaponsComponent> ent, ref DropshipTerminalWeaponsChangeScreenMsg args)
    {
        if (!Enum.IsDefined(args.Screen))
            return;

        ref var screen = ref args.First ? ref ent.Comp.ScreenOne : ref ent.Comp.ScreenTwo;
        screen.State = args.Screen;

        if (args.Screen == StrikeWeapon)
            screen.Weapon = null;

        Dirty(ent);
        RefreshWeaponsUI(ent);
    }

    private void OnWeaponsChooseWeaponMsg(Entity<DropshipTerminalWeaponsComponent> ent, ref DropshipTerminalWeaponsChooseWeaponMsg args)
    {
        if (!TryGetEntity(args.Weapon, out var weapon) ||
            !_dropship.IsWeaponAttached(weapon.Value))
        {
            return;
        }

        ref var screen = ref args.First ? ref ent.Comp.ScreenOne : ref ent.Comp.ScreenTwo;
        screen.Weapon = args.Weapon;

        if (screen.State == Equip)
            screen.State = SelectingWeapon;
        else if (screen.State == StrikeWeapon)
            screen.State = Target;

        Dirty(ent);
        RefreshWeaponsUI(ent);
    }

    private void OnWeaponsFireMsg(Entity<DropshipTerminalWeaponsComponent> ent, ref DropshipTerminalWeaponsFireMsg args)
    {
        if (_net.IsClient)
            return;

        var actor = args.Actor;
        ref var screen = ref args.First ? ref ent.Comp.ScreenOne : ref ent.Comp.ScreenTwo;
        if (screen.Weapon is not { } netWeapon)
        {
            var msg = Loc.GetString("rmc-dropship-weapons-fire-no-weapon");
            _popup.PopupCursor(msg, actor, PopupType.SmallCaution);
            return;
        }

        if (!TryGetEntity(netWeapon, out var weapon) ||
            !TryComp(weapon, out DropshipWeaponComponent? weaponComp))
        {
            screen.Weapon = null;
            Dirty(ent);
            return;
        }

        Entity<DropshipComponent> dropship = default;
        if (!_casDebug)
        {
            if (!_dropship.TryGetGridDropship(weapon.Value, out dropship))
                return;

            if (!TryComp(dropship, out FTLComponent? ftl) ||
                (ftl.State != FTLState.Travelling && ftl.State != FTLState.Arriving))
            {
                var msg = Loc.GetString("rmc-dropship-weapons-fire-not-flying");
                _popup.PopupCursor(msg, actor, PopupType.SmallCaution);
                return;
            }
        }

        if (ent.Comp.Target is not { } target)
            return;

        if (!IsValidTarget(target))
        {
            RemovePvsActors(ent);
            ent.Comp.Target = null;
            Dirty(ent);
            return;
        }

        var coordinates = _transform.GetMoverCoordinates(target).SnapToGrid(EntityManager, _mapManager);
        if (!_casDebug && !_area.CanCAS(coordinates))
        {
            var msg = Loc.GetString("rmc-laser-designator-not-cas");
            _popup.PopupCursor(msg, actor);
            return;
        }

        if (!_casDebug && weaponComp.Skills != null && !_skills.HasSkills(actor, weaponComp.Skills))
        {
            var msg = Loc.GetString("rmc-laser-designator-not-skilled");
            _popup.PopupCursor(msg, actor);
            return;
        }

        if (!_casDebug &&
            !weaponComp.FireInTransport &&
            !HasComp<DropshipInFlyByComponent>(dropship))
        {
            // TODO RMC14 fire mission only weapons
            return;
        }

        var time = _timing.CurTime;
        if (time < weaponComp.NextFireAt)
        {
            var msg = Loc.GetString("rmc-dropship-weapons-fire-cooldown", ("weapon", weapon));
            _popup.PopupCursor(msg, actor);
            return;
        }

        if (!TryGetWeaponAmmo((weapon.Value, weaponComp), out var ammo) ||
            ammo.Comp.Rounds < ammo.Comp.RoundsPerShot)
        {
            var msg = Loc.GetString("rmc-dropship-weapons-fire-no-ammo", ("weapon", weapon));
            _popup.PopupCursor(msg, actor);
            return;
        }

        if (ammo.Comp.Rounds < ammo.Comp.RoundsPerShot)
            return;

        ammo.Comp.Rounds -= ammo.Comp.RoundsPerShot;
        Dirty(ammo);

        _audio.PlayPvs(ammo.Comp.SoundCockpit, weapon.Value);
        weaponComp.NextFireAt = time + weaponComp.FireDelay;
        Dirty(weapon.Value, weaponComp);

        var spread = ammo.Comp.TargetSpread;
        var inFlight = Spawn(null, MapCoordinates.Nullspace);
        var inFlightComp = new AmmoInFlightComponent
        {
            Ammo = ammo,
            Target = coordinates.Offset(_random.NextVector2(-spread, spread + 1)),
            MarkerAt = time + ammo.Comp.TravelTime,
            ShotsLeft = ammo.Comp.RoundsPerShot,
        };

        AddComp(inFlight, inFlightComp, true);
    }

    private void OnWeaponsNightVisionMsg(Entity<DropshipTerminalWeaponsComponent> ent, ref DropshipTerminalWeaponsNightVisionMsg args)
    {
        if (_net.IsClient)
            return;

        ent.Comp.NightVision = args.On;
        if (ent.Comp.Target is { } target)
            _eye.SetDrawLight(target, !ent.Comp.NightVision);
    }

    private void OnWeaponsExitMsg(Entity<DropshipTerminalWeaponsComponent> ent, ref DropshipTerminalWeaponsExitMsg args)
    {
        ref var screen = ref args.First ? ref ent.Comp.ScreenOne : ref ent.Comp.ScreenTwo;
        screen.State = Main;

        Dirty(ent);
        RefreshWeaponsUI(ent);
    }

    private void OnWeaponsCancelMsg(Entity<DropshipTerminalWeaponsComponent> ent, ref DropshipTerminalWeaponsCancelMsg args)
    {
        ref var screen = ref args.First ? ref ent.Comp.ScreenOne : ref ent.Comp.ScreenTwo;
        screen.State = screen.State switch
        {
            Strike or StrikeWeapon => Target,
            _ => screen.State,
        };

        Dirty(ent);
        RefreshWeaponsUI(ent);
    }

    private void OnWeaponsAdjustOffset(Entity<DropshipTerminalWeaponsComponent> ent, ref DropshipTerminalWeaponsAdjustOffsetMsg args)
    {
        if (!args.Direction.IsCardinal())
            return;

        var adjust = args.Direction.ToIntVec();
        var newOffset = ent.Comp.Offset + adjust;
        var limit = ent.Comp.OffsetLimit;
        newOffset = new Vector2i(
            Math.Clamp(newOffset.X, -limit.X, limit.X),
            Math.Clamp(newOffset.Y, -limit.Y, limit.Y)
        );

        ent.Comp.Offset = newOffset;

        if (ent.Comp.Target is { } target)
            _eye.SetOffset(target, ent.Comp.Offset);

        Dirty(ent);
        RefreshWeaponsUI(ent);
    }

    private void OnWeaponsResetOffset(Entity<DropshipTerminalWeaponsComponent> ent, ref DropshipTerminalWeaponsResetOffsetMsg args)
    {
        ent.Comp.Offset = Vector2i.Zero;

        if (ent.Comp.Target is { } target)
            _eye.SetOffset(target, ent.Comp.Offset);

        Dirty(ent);
        RefreshWeaponsUI(ent);
    }

    private void OnWeaponsTargetsPrevious(Entity<DropshipTerminalWeaponsComponent> ent, ref DropshipTerminalWeaponsTargetsPreviousMsg args)
    {
        ent.Comp.TargetsPage = Math.Max(0, ent.Comp.TargetsPage - 1);
        Dirty(ent);
        RefreshWeaponsUI(ent);
    }

    private void OnWeaponsTargetsNext(Entity<DropshipTerminalWeaponsComponent> ent, ref DropshipTerminalWeaponsTargetsNextMsg args)
    {
        ent.Comp.TargetsPage = Math.Min(ent.Comp.Targets.Count % 5, ent.Comp.TargetsPage + 1);
        Dirty(ent);
        RefreshWeaponsUI(ent);
    }

    private void OnWeaponsTargetsSelect(Entity<DropshipTerminalWeaponsComponent> ent, ref DropshipTerminalWeaponsTargetsSelectMsg args)
    {
        if (!TryGetEntity(args.Target, out var target) ||
            !HasComp<DropshipTargetComponent>(target))
        {
            RefreshWeaponsUI(ent);
            return;
        }

        RemovePvsActors(ent);
        ent.Comp.Target = target;
        _eye.SetOffset(target.Value, ent.Comp.Offset);
        _eye.SetDrawLight(target.Value, !ent.Comp.NightVision);
        AddPvsActors(ent);

        RefreshWeaponsUI(ent);
        Dirty(ent);
    }

    protected virtual void RefreshWeaponsUI(Entity<DropshipTerminalWeaponsComponent> terminal)
    {
    }

    public bool TryGetWeaponAmmo(Entity<DropshipWeaponComponent?> weapon, out Entity<DropshipAmmoComponent> ammo)
    {
        ammo = default;
        if (!Resolve(weapon, ref weapon.Comp, false))
            return false;

        if (!_container.TryGetContainingContainer((weapon, null), out var container) ||
            !TryComp(container.Owner, out DropshipWeaponPointComponent? point) ||
            !_container.TryGetContainer(container.Owner, point.AmmoContainerSlotId, out var ammoContainer))
        {
            return false;
        }

        foreach (var contained in ammoContainer.ContainedEntities)
        {
            if (!TryComp(contained, out DropshipAmmoComponent? ammoComp))
                continue;

            ammo = (contained, ammoComp);
            return true;
        }

        return false;
    }

    public int GetWeaponRounds(Entity<DropshipWeaponComponent?> weapon)
    {
        if (TryGetWeaponAmmo(weapon, out var ammo))
            return ammo.Comp.Rounds;

        return 0;
    }

    private bool IsValidTarget(Entity<DropshipTargetComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        var xform = Transform(ent);
        if (!_casDebug && !HasComp<RMCPlanetComponent>(xform.GridUid))
            return false;

        return true;
    }

    public string GetUserAbbreviation(EntityUid user, int id)
    {
        var abbreviation = Loc.GetString("rmc-laser-designator-target-abbreviation", ("id", id));
        if (_squad.TryGetMemberSquad(user, out var squad))
        {
            var squadName = Name(squad);
            if (!string.IsNullOrWhiteSpace(squadName) && squadName.Length > 0)
                squadName = $"{squadName[0]}";

            abbreviation = Loc.GetString("rmc-laser-designator-target-abbreviation-squad", ("letter", squadName), ("id", id));
        }

        return abbreviation;
    }

    protected virtual void AddPvs(Entity<DropshipTerminalWeaponsComponent> terminal, Entity<ActorComponent?> actor)
    {
    }

    protected virtual void RemovePvs(Entity<DropshipTerminalWeaponsComponent> terminal, Entity<ActorComponent?> actor)
    {
    }

    private void AddPvsActors(Entity<DropshipTerminalWeaponsComponent> terminal)
    {
        foreach (var actor in _ui.GetActors(terminal.Owner, DropshipTerminalWeaponsUi.Key))
        {
            AddPvs(terminal, actor);
        }
    }

    private void RemovePvsActors(Entity<DropshipTerminalWeaponsComponent> terminal)
    {
        foreach (var actor in _ui.GetActors(terminal.Owner, DropshipTerminalWeaponsUi.Key))
        {
            RemovePvs(terminal, actor);
        }
    }

    private void StartTrackingActiveFlare(Entity<FlareSignalComponent> ent, EntityUid? user)
    {
        if (EnsureComp(ent, out ActiveFlareSignalComponent active))
            return;

        if (_net.IsClient)
            return;

        var id = ComputeNextId();
        active.Abbreviation = Loc.GetString("rmc-laser-designator-target-abbreviation", ("id", id));
        if (user != null)
            active.Abbreviation = GetUserAbbreviation(user.Value, id);
    }

    private bool IsFlareLit(EntityUid flare)
    {
        return TryComp(flare, out ExpendableLightComponent? expendable) && expendable.Activated;
    }

    private bool TryActivateSignalFlareTarget(Entity<ActiveFlareSignalComponent> ent)
    {
        if (HasComp<DropshipTargetComponent>(ent))
            return true;

        if (!IsFlareLit(ent))
            return false;

        var target = new DropshipTargetComponent { Abbreviation = ent.Comp.Abbreviation };
        AddComp(ent, target, true);
        Dirty(ent, target);

        _physics.SetBodyType(ent, BodyType.Static);

        return true;
    }

    public int ComputeNextId()
    {
        return _nextId++;
    }

    public void MakeDropshipTarget(EntityUid ent, string abbreviation)
    {
        var dropshipTarget = new DropshipTargetComponent { Abbreviation = abbreviation };
        AddComp(ent, dropshipTarget, true);
        Dirty(ent, dropshipTarget);
    }

    public override void Update(float frameTime)
    {
        var time = _timing.CurTime;
        var activeFlares = EntityQueryEnumerator<ActiveFlareSignalComponent, TransformComponent>();
        while (activeFlares.MoveNext(out var uid, out var active, out var xform))
        {
            active.LastCoordinates.Enqueue(GetNetCoordinates(xform.Coordinates));
            Dirty(uid, active);
            if (active.LastCoordinates.Count < 10)
                continue;

            active.LastCoordinates.Dequeue();
            var all = true;
            foreach (var last in active.LastCoordinates)
            {
                if (!_transform.InRange(GetCoordinates(last), xform.Coordinates, 0.01f))
                {
                    all = false;
                    break;
                }
            }

            if (!all)
                continue;

            if (!TryActivateSignalFlareTarget((uid, active)))
                continue;

            RemCompDeferred<ActiveFlareSignalComponent>(uid);
        }

        var inFlight = EntityQueryEnumerator<AmmoInFlightComponent>();
        while (inFlight.MoveNext(out var uid, out var flight))
        {
            if (_net.IsClient)
                continue;

            if (flight.MarkerAt > time)
                continue;

            if (!flight.SpawnedMarker)
            {
                flight.SpawnedMarker = true;
                flight.Marker = Spawn(DropshipTargetMarker, flight.Target);
                Dirty(uid, flight);
            }

            if (flight.MarkerAt.Add(TimeSpan.FromSeconds(1)) > time)
                continue;

            if (flight.Marker != null)
            {
                QueueDel(flight.Marker);
                flight.Marker = null;
                Dirty(uid, flight);
            }

            if (flight.NextShot > time)
                continue;

            DropshipAmmoComponent? ammo;
            if (flight.ShotsLeft > 0)
            {
                flight.ShotsLeft--;
                flight.NextShot = time + flight.ShotDelay;
                flight.SoundShotsLeft--;
                Dirty(uid, flight);

                if (!TryComp(flight.Ammo, out ammo))
                {
                    QueueDel(flight.Marker);
                    QueueDel(uid);
                    continue;
                }

                _damageables.Clear();
                var spread = _random.NextVector2(-ammo.BulletSpread, ammo.BulletSpread + 1);
                var target = _transform.ToMapCoordinates(flight.Target).Offset(spread);
                Spawn(ammo.ImpactEffect, target, rotation: _random.NextAngle());

                _entityLookup.GetEntitiesInRange(target, 0.49f, _damageables, LookupFlags.Uncontained);
                foreach (var damageable in _damageables)
                {
                    _damageable.TryChangeDamage(
                        damageable,
                        ammo.Damage,
                        damageable: damageable,
                        armorPiercing: ammo.ArmorPiercing
                    );
                }

                if (flight.SoundShotsLeft <= 0)
                {
                    flight.SoundShotsLeft = flight.SoundEveryShots;
                    _audio.PlayPvs(ammo.SoundImpact, flight.Target);
                }

                continue;
            }

            if (!TryComp(flight.Ammo, out ammo))
            {
                QueueDel(flight.Marker);
                QueueDel(uid);
                continue;
            }

            flight.PlayGroundSoundAt ??= _timing.CurTime + ammo.SoundTravelTime;
            Dirty(uid, flight);

            if (time >= flight.PlayGroundSoundAt)
            {
                _audio.PlayPvs(ammo.SoundGround, flight.Target);
                QueueDel(uid);
            }
        }

        var query = EntityQueryEnumerator<ActiveLaserDesignatorComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var active, out var xform))
        {
            if (!_transform.InRange(xform.Coordinates, active.Origin, 0.1f))
                RemCompDeferred<ActiveLaserDesignatorComponent>(uid);
        }
    }
}
