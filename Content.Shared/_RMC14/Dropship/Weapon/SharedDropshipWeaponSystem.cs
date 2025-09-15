using System.Numerics;
using System.Runtime.InteropServices;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Atmos;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Dropship.AttachmentPoint;
using Content.Shared._RMC14.Dropship.Utility.Components;
using Content.Shared._RMC14.Dropship.Utility.Systems;
using Content.Shared._RMC14.Explosion;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Medical.MedevacStretcher;
using Content.Shared._RMC14.OnCollide;
using Content.Shared._RMC14.PowerLoader;
using Content.Shared._RMC14.Rangefinder;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.Weapons.Ranged;
using Content.Shared.Administration.Logs;
using Content.Shared.Coordinates;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Examine;
using Content.Shared.Explosion.EntitySystems;
using Content.Shared.GameTicking;
using Content.Shared.IgnitionSource;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Light.Components;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.ParaDrop;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
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
using Robust.Shared.Utility;
using static Content.Shared._RMC14.Dropship.Weapon.DropshipTerminalWeaponsComponent;
using static Content.Shared._RMC14.Dropship.Weapon.DropshipTerminalWeaponsScreen;

namespace Content.Shared._RMC14.Dropship.Weapon;

public abstract class SharedDropshipWeaponSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly AreaSystem _area = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedDoorSystem _door = default!;
    [Dependency] private readonly SharedDropshipSystem _dropship = default!;
    [Dependency] private readonly DropshipUtilitySystem _dropshipUtility = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly NameModifierSystem _name = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedOnCollideSystem _onCollide = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly PowerLoaderSystem _powerloader = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedRMCFlammableSystem _rmcFlammable = default!;
    [Dependency] private readonly SharedRMCExplosionSystem _rmcExplosion = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly SquadSystem _squad = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    private static readonly EntProtoId DropshipTargetMarker = "RMCLaserDropshipTarget";

    public bool CasDebug { get; private set; }
    private readonly HashSet<Entity<DamageableComponent>> _damageables = new();
    private readonly List<EntityUid> _targetsToRemove = new();
    private int _nextId = 1;

    public override void Initialize()
    {
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);

        SubscribeLocalEvent<FlareSignalComponent, IgnitionEvent>(OnFlareSignalIgnition);
        SubscribeLocalEvent<FlareSignalComponent, GettingPickedUpAttemptEvent>(OnFlareSignalPickupAttempt);
        SubscribeLocalEvent<FlareSignalComponent, ExaminedEvent>(OnFlareSignalExamined);
        SubscribeLocalEvent<FlareSignalComponent, DroppedEvent>(OnFlareSignalDropped);
        SubscribeLocalEvent<FlareSignalComponent, ThrownEvent>(OnFlareSignalThrown);
        SubscribeLocalEvent<FlareSignalComponent, GrenadeContentThrownEvent>(OnFlareSignalGrenadeContentThrown);
        SubscribeLocalEvent<FlareSignalComponent, StopThrowEvent>(OnFlareSignalStopThrow);
        SubscribeLocalEvent<FlareSignalComponent, ContainerGettingInsertedAttemptEvent>(OnFlareSignalContainerGettingInsertedAttempt);

        SubscribeLocalEvent<DropshipTerminalWeaponsComponent, MapInitEvent>(OnTerminalMapInit);
        SubscribeLocalEvent<DropshipTerminalWeaponsComponent, BoundUIOpenedEvent>(OnTerminalBUIOpened);
        SubscribeLocalEvent<DropshipTerminalWeaponsComponent, BoundUIClosedEvent>(OnTerminalBUIClosed);

        SubscribeLocalEvent<DropshipTargetComponent, MapInitEvent>(OnDropshipTargetMapInit);
        SubscribeLocalEvent<DropshipTargetComponent, ComponentRemove>(OnDropshipTargetRemove);
        SubscribeLocalEvent<DropshipTargetComponent, EntityTerminatingEvent>(OnDropshipTargetRemove);
        SubscribeLocalEvent<DropshipTargetComponent, ExaminedEvent>(OnActiveFlareExamined);

        SubscribeLocalEvent<ActiveFlareSignalComponent, RefreshNameModifiersEvent>(OnRefreshNameModifier);

        SubscribeLocalEvent<DropshipTargetEyeComponent, ComponentRemove>(OnDropshipTargetEyeRemove);
        SubscribeLocalEvent<DropshipTargetEyeComponent, EntityTerminatingEvent>(OnDropshipTargetEyeRemove);

        SubscribeLocalEvent<DropshipAmmoComponent, ExaminedEvent>(OnAmmoExamined);
        SubscribeLocalEvent<DropshipAmmoComponent, PowerLoaderInteractEvent>(OnAmmoInteract);

        Subs.BuiEvents<DropshipTerminalWeaponsComponent>(DropshipTerminalWeaponsUi.Key,
            subs =>
            {
                subs.Event<DropshipTerminalWeaponsChangeScreenMsg>(OnWeaponsChangeScreenMsg);
                subs.Event<DropshipTerminalWeaponsChooseWeaponMsg>(OnWeaponsChooseWeaponMsg);
                subs.Event<DropshipTerminalWeaponsChooseMedevacMsg>(OnWeaponsChooseMedevacMsg);
                subs.Event<DropshipTerminalWeaponsChooseFultonMsg>(OnWeaponsChooseFultonMsg);
                subs.Event<DropshipTerminalWeaponsChooseParaDropMsg>(OnWeaponsChooseParaDropMsg);
                subs.Event<DropshipTerminalWeaponsFireMsg>(OnWeaponsFireMsg);
                subs.Event<DropshipTerminalWeaponsNightVisionMsg>(OnWeaponsNightVisionMsg);
                subs.Event<DropshipTerminalWeaponsExitMsg>(OnWeaponsExitMsg);
                subs.Event<DropshipTerminalWeaponsCancelMsg>(OnWeaponsCancelMsg);
                subs.Event<DropshipTerminalWeaponsAdjustOffsetMsg>(OnWeaponsAdjustOffset);
                subs.Event<DropshipTerminalWeaponsResetOffsetMsg>(OnWeaponsResetOffset);
                subs.Event<DropshipTerminalWeaponsTargetsPreviousMsg>(OnWeaponsTargetsPrevious);
                subs.Event<DropshipTerminalWeaponsTargetsNextMsg>(OnWeaponsTargetsNext);
                subs.Event<DropshipTerminalWeaponsTargetsSelectMsg>(OnWeaponsTargetsSelect);
                subs.Event<DropshipTerminalWeaponsMedevacPreviousMsg>(OnWeaponsMedevacPrevious);
                subs.Event<DropshipTerminalWeaponsMedevacNextMsg>(OnWeaponsMedevacNext);
                subs.Event<DropshipTerminalWeaponsMedevacSelectMsg>(OnWeaponsMedevacSelect);
                subs.Event<DropshipTerminalWeaponsFultonPreviousMsg>(OnWeaponsFultonPrevious);
                subs.Event<DropshipTerminalWeaponsFultonNextMsg>(OnWeaponsFultonNext);
                subs.Event<DropshipTerminalWeaponsFultonSelectMsg>(OnWeaponsFultonSelect);
                subs.Event<DropShipTerminalWeaponsParaDropTargetSelectMsg>(OnWeaponsParaDropSelect);
            });

        Subs.CVar(_config, RMCCVars.RMCDropshipCASDebug, v => CasDebug = v, true);
    }

    private void SetTarget(Entity<DropshipTerminalWeaponsComponent> dropshipWeaponsTerminal, EntityUid? newTarget)
    {
        if (newTarget == dropshipWeaponsTerminal.Comp.Target)
            return;

        if (!_dropship.TryGetGridDropship(dropshipWeaponsTerminal, out var dropship))
            return;

        dropshipWeaponsTerminal.Comp.Target = newTarget;
        Dirty(dropshipWeaponsTerminal);

        var ev = new DropshipTargetChangedEvent(GetNetEntity(newTarget));
        foreach (var attachmentPoint in dropship.Comp.AttachmentPoints)
        {
            RaiseLocalEvent(attachmentPoint, ev);
        }
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
        if (!IsFlareLit(ent))
            return;

        StartTrackingActiveFlare(ent, args.User);
    }

    private void OnFlareSignalThrown(Entity<FlareSignalComponent> ent, ref ThrownEvent args)
    {
        if (!IsFlareLit(ent))
            return;

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

    private void OnFlareSignalGrenadeContentThrown(Entity<FlareSignalComponent> ent, ref GrenadeContentThrownEvent args)
    {
        if (!TryComp(args.Source, out ProjectileComponent? projectile))
            return;

        var id = ComputeNextId();
        var abbreviation = Loc.GetString("rmc-laser-designator-target-abbreviation", ("id", id));
        if (projectile.Shooter != null)
            abbreviation = GetUserAbbreviation(projectile.Shooter.Value, id);

        if (projectile.Weapon != null)
        {
            if (TryComp(projectile.Weapon, out RMCAirShotComponent? airShot))
            {
                airShot.LastFlareId = abbreviation;
                Dirty(projectile.Weapon.Value, airShot);
            }
        }

        MakeDropshipTarget(ent, abbreviation);
        _physics.SetBodyType(ent, BodyType.Static);
    }

    private void OnActiveFlareExamined(Entity<DropshipTargetComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.Abbreviation is { } id)
            args.PushMarkup(Loc.GetString("rmc-laser-designator-signal-flare-examine-id", ("id", id)));
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
        var netEnt = GetNetEntity(ent);
        var terminals = EntityQueryEnumerator<DropshipTerminalWeaponsComponent>();
        while (terminals.MoveNext(out var uid, out var terminal))
        {
            var targets = terminal.Targets;
            if (HasComp<MedevacStretcherComponent>(ent))
                targets = terminal.Medevacs;
            else if (HasComp<RMCActiveFultonComponent>(ent))
                targets = terminal.Fultons;

            targets.Add(new TargetEnt(netEnt, ent.Comp.Abbreviation));
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
                SetTarget((uid, terminal), null);
            }

            var targets = terminal.Targets;
            if (HasComp<MedevacStretcherComponent>(ent))
                targets = terminal.Medevacs;
            else if (HasComp<RMCActiveFultonComponent>(ent))
                targets = terminal.Fultons;

            var span = CollectionsMarshal.AsSpan(targets);
            for (var i = 0; i < span.Length; i++)
            {
                ref var target = ref span[i];
                if (target.Id != netUid)
                    continue;

                targets.RemoveAt(i);
                break;
            }

            Dirty(uid, terminal);
        }

        if (_net.IsClient)
            return;

        foreach (var (_, eye) in ent.Comp.Eyes)
        {
            QueueDel(eye);
        }
    }

    private void OnDropshipTargetEyeRemove<T>(Entity<DropshipTargetEyeComponent> ent, ref T args)
    {
        if (TerminatingOrDeleted(ent.Comp.Target) ||
            !TryComp(ent.Comp.Target, out DropshipTargetComponent? target))
        {
            return;
        }

        _targetsToRemove.Clear();
        foreach (var (terminal, eye) in target.Eyes)
        {
            if (eye == ent.Owner)
                _targetsToRemove.Add(terminal);
        }

        foreach (var remove in _targetsToRemove)
        {
            target.Eyes.Remove(remove);
        }
    }

    private void OnAmmoExamined(Entity<DropshipAmmoComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(DropshipAmmoComponent)))
        {
            args.PushText(Loc.GetString("rmc-dropship-ammo-examine", ("rounds", ent.Comp.Rounds)));
        }
    }

    private void OnAmmoInteract(Entity<DropshipAmmoComponent> ent, ref PowerLoaderInteractEvent args)
    {
        if (args.Handled)
            return;

        // TODO RMC14 make this a whitelist check, not an id check
        if (TryComp(args.Target, out DropshipWeaponPointComponent? point) &&
            _container.TryGetContainer(args.Target, point.WeaponContainerSlotId, out var container) &&
            container.ContainedEntities.TryFirstOrNull(out var weapon) &&
            ent.Comp.Weapon.Id != Prototype(weapon.Value)?.ID)
        {
            args.Handled = true;
            foreach (var buckled in args.Buckled)
            {
                _popup.PopupClient(Loc.GetString("rmc-power-loader-wrong-weapon"), args.Target, buckled, PopupType.SmallCaution);
            }

            return;
        }

        if (!TryComp<DropshipAmmoComponent>(args.Target, out var otherAmmo))
            return;

        args.Handled = true;

        if (ent.Comp.AmmoType != otherAmmo.AmmoType)
        {
            foreach (var buckled in args.Buckled)
            {
                _popup.PopupClient(Loc.GetString("rmc-power-loader-wrong-ammo"), args.Target, buckled, PopupType.SmallCaution);
            }

            return;
        }

        if (otherAmmo.Rounds == otherAmmo.MaxRounds)
        {
            foreach (var buckled in args.Buckled)
            {
                _popup.PopupClient(Loc.GetString("rmc-power-loader-full-ammo", ("ammo", args.Target)), args.Target, buckled, PopupType.SmallCaution);
            }

            return;
        }

        var roundsToFill = Math.Min(ent.Comp.Rounds, otherAmmo.MaxRounds - otherAmmo.Rounds);

        ent.Comp.Rounds -= roundsToFill;
        otherAmmo.Rounds += roundsToFill;

        _appearance.SetData(ent, DropshipAmmoVisuals.Fill, ent.Comp.Rounds);
        _appearance.SetData(args.Target, DropshipAmmoVisuals.Fill, otherAmmo.Rounds);

        Dirty(ent);
        Dirty(args.Target, otherAmmo);

        if (ent.Comp.Rounds <= 0)
        {
            if (_net.IsServer)
                QueueDel(args.Used);

            _container.TryRemoveFromContainer(args.Used, true);
            _powerloader.TrySyncHands(args.PowerLoader);
        }

        foreach (var buckled in args.Buckled)
        {
            _popup.PopupClient(Loc.GetString("rmc-power-loader-transfer-ammo", ("rounds", roundsToFill), ("ammo", args.Target)), args.Target, buckled);
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

    private void OnWeaponsChooseMedevacMsg(Entity<DropshipTerminalWeaponsComponent> ent, ref DropshipTerminalWeaponsChooseMedevacMsg args)
    {
        SetScreenUtility<MedevacComponent>(ent, args.First, Medevac);
    }

    private void OnWeaponsChooseFultonMsg(Entity<DropshipTerminalWeaponsComponent> ent, ref DropshipTerminalWeaponsChooseFultonMsg args)
    {
        SetScreenUtility<RMCFultonComponent>(ent, args.First, Fulton);
    }

    private void OnWeaponsChooseParaDropMsg(Entity<DropshipTerminalWeaponsComponent> ent, ref DropshipTerminalWeaponsChooseParaDropMsg args)
    {
        SetScreenUtility<RMCParaDropComponent>(ent, args.First, Paradrop);
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
        if (!CasDebug)
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
            SetTarget(ent, null);
            Dirty(ent);
            return;
        }

        var coordinates = _transform.GetMoverCoordinates(target).SnapToGrid(EntityManager, _mapManager);
        if (!CasDebug && !_area.CanCAS(coordinates))
        {
            var msg = Loc.GetString("rmc-laser-designator-not-cas");
            _popup.PopupCursor(msg, actor);
            return;
        }

        if (!CasDebug && weaponComp.Skills != null && !_skills.HasSkills(actor, weaponComp.Skills))
        {
            var msg = Loc.GetString("rmc-laser-designator-not-skilled");
            _popup.PopupCursor(msg, actor);
            return;
        }

        if (!CasDebug &&
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
        _appearance.SetData(ammo, DropshipAmmoVisuals.Fill, ammo.Comp.Rounds);
        Dirty(ammo);

        _audio.PlayPvs(ammo.Comp.SoundCockpit, weapon.Value);
        weaponComp.NextFireAt = time + weaponComp.FireDelay;
        Dirty(weapon.Value, weaponComp);

        var spread = ammo.Comp.TargetSpread;
        var targetCoords = coordinates;
        if (spread != 0)
            targetCoords = targetCoords.Offset(_random.NextVector2(-spread, spread + 1));

        var inFlight = Spawn(null, MapCoordinates.Nullspace);
        var inFlightComp = new AmmoInFlightComponent
        {
            Target = targetCoords,
            MarkerAt = time + ammo.Comp.TravelTime,
            ShotsLeft = ammo.Comp.RoundsPerShot,
            ShotsPerVolley = ammo.Comp.ShotsPerVolley,
            Damage = ammo.Comp.Damage,
            ArmorPiercing = ammo.Comp.ArmorPiercing,
            BulletSpread = ammo.Comp.BulletSpread,
            SoundTravelTime = ammo.Comp.SoundTravelTime,
            SoundMarker = ammo.Comp.SoundMarker,
            SoundGround = ammo.Comp.SoundGround,
            SoundImpact = ammo.Comp.SoundImpact,
            ImpactEffect = ammo.Comp.ImpactEffect,
            Explosion = ammo.Comp.Explosion,
            Fire = ammo.Comp.Fire,
            SoundEveryShots = ammo.Comp.SoundEveryShots,
        };

        AddComp(inFlight, inFlightComp, true);

        if (ammo.Comp.DeleteOnEmpty && ammo.Comp.Rounds <= 0)
            QueueDel(ammo);

        _adminLog.Add(LogType.RMCDropshipWeapon, $"{ToPrettyString(args.Actor)} fired {ToPrettyString(weapon)} at {ToPrettyString(target)}");
    }

    private void OnWeaponsNightVisionMsg(Entity<DropshipTerminalWeaponsComponent> ent, ref DropshipTerminalWeaponsNightVisionMsg args)
    {
        if (_net.IsClient)
            return;

        ent.Comp.NightVision = args.On;
        if (EnsureTargetEye(ent, ent.Comp.Target) is { } target)
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

        if (EnsureTargetEye(ent, ent.Comp.Target) is { } target)
            _eye.SetOffset(target, ent.Comp.Offset);

        Dirty(ent);
        RefreshWeaponsUI(ent);
    }

    private void OnWeaponsResetOffset(Entity<DropshipTerminalWeaponsComponent> ent, ref DropshipTerminalWeaponsResetOffsetMsg args)
    {
        ent.Comp.Offset = Vector2i.Zero;

        if (EnsureTargetEye(ent, ent.Comp.Target) is { } target)
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
        ent.Comp.TargetsPage = Math.Min(ent.Comp.Targets.Count / 5, ent.Comp.TargetsPage + 1);
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

        UpdateTarget(ent, target.Value);
    }

    private void OnWeaponsMedevacPrevious(Entity<DropshipTerminalWeaponsComponent> ent, ref DropshipTerminalWeaponsMedevacPreviousMsg args)
    {
        ent.Comp.MedevacsPage = Math.Max(0, ent.Comp.MedevacsPage - 1);
        Dirty(ent);
        RefreshWeaponsUI(ent);
    }

    private void OnWeaponsMedevacNext(Entity<DropshipTerminalWeaponsComponent> ent, ref DropshipTerminalWeaponsMedevacNextMsg args)
    {
        ent.Comp.MedevacsPage = Math.Min(ent.Comp.Medevacs.Count % 5, ent.Comp.MedevacsPage + 1);
        Dirty(ent);
        RefreshWeaponsUI(ent);
    }

    private void OnWeaponsMedevacSelect(Entity<DropshipTerminalWeaponsComponent> ent, ref DropshipTerminalWeaponsMedevacSelectMsg args)
    {
        if (!TryGetEntity(args.Target, out var target) ||
            !HasComp<MedevacStretcherComponent>(target))
        {
            RefreshWeaponsUI(ent);
            return;
        }

        UpdateTarget(ent, target.Value);
        _popup.PopupClient("You move your dropship above the selected stretcher's beacon. You can now manually activate the medevac system to hoist the patient up.", args.Actor);
    }

    private void OnWeaponsFultonPrevious(Entity<DropshipTerminalWeaponsComponent> ent, ref DropshipTerminalWeaponsFultonPreviousMsg args)
    {
        ent.Comp.FultonsPage = Math.Max(0, ent.Comp.FultonsPage - 1);
        Dirty(ent);
        RefreshWeaponsUI(ent);
    }

    private void OnWeaponsFultonNext(Entity<DropshipTerminalWeaponsComponent> ent, ref DropshipTerminalWeaponsFultonNextMsg args)
    {
        ent.Comp.FultonsPage = Math.Min(ent.Comp.Fultons.Count % 5, ent.Comp.FultonsPage + 1);
        Dirty(ent);
        RefreshWeaponsUI(ent);
    }

    private void OnWeaponsFultonSelect(Entity<DropshipTerminalWeaponsComponent> ent, ref DropshipTerminalWeaponsFultonSelectMsg args)
    {
        if (_net.IsClient)
            return;

        if (!TryGetEntity(args.Target, out var target) ||
            !HasComp<RMCActiveFultonComponent>(target))
        {
            RefreshWeaponsUI(ent);
            return;
        }

        if (!_dropship.TryGetGridDropship(ent, out var dropship) ||
            dropship.Comp.AttachmentPoints.Count == 0)
        {
            return;
        }

        foreach (var point in dropship.Comp.AttachmentPoints)
        {
            if (!TryComp(point, out DropshipUtilityPointComponent? utilityComp) ||
                !_container.TryGetContainer(point, utilityComp.UtilitySlotId, out var container) ||
                container.ContainedEntities.Count == 0)
            {
                continue;
            }

            var contained = container.ContainedEntities[0];
            if (!HasComp<RMCFultonComponent>(contained))
                continue;

            if (TryComp(contained, out DropshipUtilityComponent? utility) &&
                !_dropshipUtility.IsActivatable((contained, utility), args.Actor, out var popup))
            {
                _popup.PopupCursor(popup, args.Actor);
                continue;
            }

            RemComp<DropshipTargetComponent>(target.Value);
            RemCompDeferred<RMCActiveFultonComponent>(target.Value);
            _transform.PlaceNextTo(target.Value, point);
            RefreshWeaponsUI(ent);
            return;
        }

        RefreshWeaponsUI(ent);
    }

    private void OnWeaponsParaDropSelect(Entity<DropshipTerminalWeaponsComponent> ent, ref DropShipTerminalWeaponsParaDropTargetSelectMsg args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (ent.Comp.Target == null)
        {
            if (_net.IsClient)
            {
                var msg = Loc.GetString("rmc-dropship-paradrop-lock-no-target");
                _popup.PopupCursor(msg, args.Actor, PopupType.SmallCaution);
            }
            RefreshWeaponsUI(ent);
            return;
        }

        if (!_dropship.TryGetGridDropship(ent, out var dropship) ||
            dropship.Comp.AttachmentPoints.Count == 0)
            return;

        // Only select a target while travelling
        if (!CasDebug)
        {
            if (!TryComp(dropship, out FTLComponent? ftl) ||
                ftl.State != FTLState.Travelling)
            {
                if (_net.IsClient)
                {
                    var msg = Loc.GetString("rmc-dropship-paradrop-lock-target-not-flying");
                    _popup.PopupCursor(msg, args.Actor, PopupType.SmallCaution);
                }
                return;
            }
        }

        if (!IsValidTarget(ent.Comp.Target.Value))
        {
            RemovePvsActors(ent);
            SetTarget(ent, null);
            Dirty(ent);
            return;
        }

        var coordinates = _transform.GetMoverCoordinates(ent.Comp.Target.Value);

        // Can't drop underground
        if (!CasDebug)
        {
            if(!_area.CanCAS(coordinates))
            {
                if (_net.IsClient)
                {
                    var msg = Loc.GetString("rmc-laser-designator-not-cas");
                    _popup.PopupCursor(msg, args.Actor);
                }
                return;
            }
        }

        // Open the doors so people can jump out
        if (args.On)
        {
            var enumerator = Transform(dropship).ChildEnumerator;
            while (enumerator.MoveNext(out var child))
            {
                if (!TryComp(child, out DoorComponent? door) ||
                    door.Location != DoorLocation.Aft ||
                    !TryComp(child, out DoorBoltComponent? doorBolt) ||
                    door.State == DoorState.Open)
                    continue;

                _door.StartOpening(child);
                _door.TrySetBoltDown((child,doorBolt), true);
            }

            var paraDrop = EnsureComp<ActiveParaDropComponent>(dropship);
            paraDrop.DropTarget = ent.Comp.Target;
            Dirty(dropship, paraDrop);
        }
        else
        {
            RemComp<ActiveParaDropComponent>(dropship);
        }
        RefreshWeaponsUI(ent);
    }

    private void OnRefreshNameModifier(Entity<ActiveFlareSignalComponent> ent, ref RefreshNameModifiersEvent args)
    {
        if (ent.Comp.Abbreviation == null)
            return;

        args.AddModifier(ent.Comp.Abbreviation);
    }

    private void UpdateTarget(Entity<DropshipTerminalWeaponsComponent> ent, EntityUid target)
    {
        RemovePvsActors(ent);
        SetTarget(ent, target);

        if (EnsureTargetEye(ent, ent.Comp.Target) is { } targetEye)
        {
            _eye.SetOffset(targetEye, ent.Comp.Offset);
            _eye.SetDrawLight(targetEye, !ent.Comp.NightVision);
        }

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

    /// <summary>
    /// Checks if a target is valid for being fired upon
    /// </summary>
    private bool IsValidTarget(Entity<DropshipTargetComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        var xform = Transform(ent);
        if (!CasDebug && !HasComp<RMCPlanetComponent>(xform.GridUid))
            return false;
        if (!ent.Comp.IsTargetableByWeapons)
        {
            return false;
        }
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

        if (ent.Comp.Abbreviation == null)
            return false;

        var target = new DropshipTargetComponent { Abbreviation = ent.Comp.Abbreviation };
        AddComp(ent, target, true);
        Dirty(ent, target);

        _name.RefreshNameModifiers(ent.Owner);
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

                _audio.PlayPvs(flight.SoundMarker, flight.Marker.Value);
            }

            if (flight.MarkerAt.Add(TimeSpan.FromSeconds(1)) > time)
                continue;

            if (flight.Marker != null)
            {
                if (_net.IsServer)
                    QueueDel(flight.Marker);

                flight.Marker = null;
                Dirty(uid, flight);
            }

            if (flight.NextShot > time)
                continue;

            if (flight.ShotsLeft > 0)
            {
                flight.ShotsLeft -= flight.ShotsPerVolley;
                flight.NextShot = time + flight.ShotDelay;
                flight.SoundShotsLeft--;
                Dirty(uid, flight);

                var spread = Vector2.Zero;
                if (flight.BulletSpread > 0)
                    spread = _random.NextVector2(-flight.BulletSpread, flight.BulletSpread + 1);

                var target = _transform.ToMapCoordinates(flight.Target).Offset(spread);
                if (flight.ImpactEffect != null)
                    Spawn(flight.ImpactEffect, target, rotation: _random.NextAngle());

                if (flight.Damage != null)
                {
                    _damageables.Clear();

                    _entityLookup.GetEntitiesInRange(target, 0.49f, _damageables, LookupFlags.Uncontained);
                    foreach (var damageable in _damageables)
                    {
                        _damageable.TryChangeDamage(
                            damageable,
                            flight.Damage,
                            damageable: damageable,
                            armorPiercing: flight.ArmorPiercing
                        );
                    }
                }

                if (flight.Explosion != null)
                {
                    _rmcExplosion.QueueExplosion(target,
                        flight.Explosion.Type,
                        flight.Explosion.Total,
                        flight.Explosion.Slope,
                        flight.Explosion.Max,
                        uid
                    );
                }

                if (flight.Fire != null)
                {
                    var chain = _onCollide.SpawnChain();
                    if (flight.Fire.Total is { } total)
                    {
                        var tiles = new List<Vector2i>();
                        for (var x = -flight.Fire.Range; x <= flight.Fire.Range; x++)
                        {
                            for (var y = -flight.Fire.Range; y <= flight.Fire.Range; y++)
                            {
                                tiles.Add((x, y));
                            }
                        }

                        for (var i = 0; i < total; i++)
                        {
                            if (tiles.Count == 0)
                                break;

                            var tile = _random.PickAndTake(tiles);
                            var coords = flight.Target.Offset(tile);
                            _rmcFlammable.SpawnFire(coords,
                                flight.Fire.Type,
                                chain,
                                flight.Fire.Range,
                                flight.Fire.Intensity,
                                flight.Fire.Duration,
                                out _
                            );
                        }
                    }
                    else
                    {
                        for (var x = -flight.Fire.Range; x <= flight.Fire.Range; x++)
                        {
                            for (var y = -flight.Fire.Range; y <= flight.Fire.Range; y++)
                            {
                                var coords = flight.Target.Offset(new Vector2(x, y));
                                _rmcFlammable.SpawnFire(coords,
                                    flight.Fire.Type,
                                    chain,
                                    flight.Fire.Range,
                                    flight.Fire.Intensity,
                                    flight.Fire.Duration,
                                    out _
                                );
                            }
                        }
                    }
                }

                if (flight.SoundShotsLeft <= 0)
                {
                    flight.SoundShotsLeft = flight.SoundEveryShots;
                    _audio.PlayPvs(flight.SoundImpact, flight.Target);
                }

                continue;
            }

            flight.PlayGroundSoundAt ??= _timing.CurTime + flight.SoundTravelTime;
            Dirty(uid, flight);

            if (time >= flight.PlayGroundSoundAt)
            {
                _audio.PlayPvs(flight.SoundGround, flight.Target);
                if (_net.IsServer)
                    QueueDel(uid);
            }
        }

        var query = EntityQueryEnumerator<ActiveLaserDesignatorComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var active, out var xform))
        {
            if (!_transform.InRange(xform.Coordinates, active.Origin, active.BreakRange))
                RemCompDeferred<ActiveLaserDesignatorComponent>(uid);
        }
    }

    public void TargetUpdated(Entity<DropshipTargetComponent> ent)
    {
        var terminals = EntityQueryEnumerator<DropshipTerminalWeaponsComponent>();
        while (terminals.MoveNext(out var uid, out var terminal))
        {
            var span = CollectionsMarshal.AsSpan(terminal.Medevacs);
            for (var i = 0; i < span.Length; i++)
            {
                ref var target = ref span[i];
                if (target.Id != GetNetEntity(ent.Owner))
                    continue;

                target = target with { Name = ent.Comp.Abbreviation };
                break;
            }

            Dirty(uid, terminal);
        }
    }

    private EntityUid? EnsureTargetEye(Entity<DropshipTerminalWeaponsComponent> terminal, Entity<DropshipTargetComponent?>? targetNullable)
    {
        if (targetNullable == null)
            return null;

        var target = targetNullable.Value;
        if (!Resolve(target, ref target.Comp, false))
            return null;

        if (!target.Comp.Eyes.TryGetValue(terminal, out var eye))
        {
            if (_net.IsClient)
                return null;

            eye = Spawn(null, target.Owner.ToCoordinates());
            target.Comp.Eyes[terminal] = eye;
            Dirty(target);

            var eyeComp = EnsureComp<EyeComponent>(eye);
            _eye.SetDrawFov(eye, false, eyeComp);

            var targetEyeComp = EnsureComp<DropshipTargetEyeComponent>(eye);
            targetEyeComp.Target = target;
            Dirty(eye, targetEyeComp);
        }

        return eye;
    }

    public bool TryGetTargetEye(
        Entity<DropshipTerminalWeaponsComponent> terminal,
        Entity<DropshipTargetComponent?> target,
        out EntityUid eye)
    {
        if (Resolve(target, ref target.Comp, false) &&
            target.Comp.Eyes.TryGetValue(terminal, out eye))
        {
            return true;
        }

        eye = default;
        return false;
    }

    public void MakeTarget(EntityUid target, string abbreviation, bool targetableByWeapons)
    {
        var targetComp = new DropshipTargetComponent()
        {
            Abbreviation = abbreviation,
            IsTargetableByWeapons = targetableByWeapons,
        };

        AddComp(target, targetComp, true);
    }

    private void SetScreenUtility<T>(Entity<DropshipTerminalWeaponsComponent> ent, bool first, DropshipTerminalWeaponsScreen state) where T : IComponent
    {
        if (!_dropship.TryGetGridDropship(ent, out var dropship) ||
            dropship.Comp.AttachmentPoints.Count == 0)
        {
            return;
        }

        var hasUtility = false;
        foreach (var point in dropship.Comp.AttachmentPoints)
        {
            if (!TryComp(point, out DropshipUtilityPointComponent? utilityComp) ||
                !_container.TryGetContainer(point, utilityComp.UtilitySlotId, out var container) ||
                container.ContainedEntities.Count == 0)
            {
                continue;
            }

            foreach (var contained in container.ContainedEntities)
            {
                if (!HasComp<T>(contained))
                    continue;

                hasUtility = true;
                break;
            }
        }

        if (!hasUtility)
            return;

        ref var screen = ref first ? ref ent.Comp.ScreenOne : ref ent.Comp.ScreenTwo;
        screen.State = state;

        Dirty(ent);
        RefreshWeaponsUI(ent);
    }
}
