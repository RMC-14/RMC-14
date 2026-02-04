using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Atmos;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Dropship.AttachmentPoint;
using Content.Shared._RMC14.Dropship.ElectronicSystem;
using Content.Shared._RMC14.Dropship.FireMission;
using Content.Shared._RMC14.Dropship.Utility.Components;
using Content.Shared._RMC14.Dropship.Utility.Systems;
using Content.Shared._RMC14.Explosion;
using Content.Shared._RMC14.Explosion.Implosion;
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
using Content.Shared.Chat;
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
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Spawners;
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
    [Dependency] private readonly ISharedChatManager _chat = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedDoorSystem _door = default!;
    [Dependency] private readonly SharedDropshipSystem _dropship = default!;
    [Dependency] private readonly SharedRMCEquipmentDeployerSystem _equipmentDeployer = default!;
    [Dependency] private readonly DropshipUtilitySystem _dropshipUtility = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly FireMissionSystem _fireMission = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly NameModifierSystem _name = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedOnCollideSystem _onCollide = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly SharedPointLightSystem _pointLight = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly PowerLoaderSystem _powerloader = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedRMCFlammableSystem _rmcFlammable = default!;
    [Dependency] private readonly SharedRMCExplosionSystem _rmcExplosion = default!;
    [Dependency] private readonly RMCImplosionSystem _rmcImplosion = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly SquadSystem _squad = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    private static readonly EntProtoId DropshipTargetMarker = "RMCLaserDropshipTarget";
    private const string SpotlightState = "spotlights_";
    private const float DefaultMarkerDuration = 1;

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

        SubscribeLocalEvent<ActivateDropshipWeaponOnSpawnComponent, MapInitEvent>(OnDropshipWeaponOnSpawnFire);

        Subs.BuiEvents<DropshipTerminalWeaponsComponent>(DropshipTerminalWeaponsUi.Key,
            subs =>
            {
                subs.Event<DropshipTerminalWeaponsChangeScreenMsg>(OnWeaponsChangeScreenMsg);
                subs.Event<DropshipTerminalWeaponsChooseWeaponMsg>(OnWeaponsChooseWeaponMsg);
                subs.Event<DropshipTerminalWeaponsChooseMedevacMsg>(OnWeaponsChooseMedevacMsg);
                subs.Event<DropshipTerminalWeaponsChooseFultonMsg>(OnWeaponsChooseFultonMsg);
                subs.Event<DropshipTerminalWeaponsChooseParaDropMsg>(OnWeaponsChooseParaDropMsg);
                subs.Event<DropshipTerminalWeaponsChooseSpotlightMsg>(OnWeaponsChooseSpotlightMsg);
                subs.Event<DropshipTerminalWeaponsChooseEquipmentDeployerMsg>(OnWeaponsChooseEquipmentDeployerMsg);
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
                subs.Event<DropShipTerminalWeaponsSpotlightToggleMsg>(OnWeaponsSpotlightSelect);
                subs.Event<DropShipTerminalWeaponsEquipmentDeployToggleMsg>(OnEquipmentDeploy);
                subs.Event<DropShipTerminalWeaponsEquipmentAutoDeployToggleMsg>(OnEquipmentToggleAutoDeploy);
                subs.Event<DropshipTerminalWeaponsCreateFireMissionMsg>(OnCreateFireMission);
                subs.Event<DropshipTerminalWeaponsViewFireMissionMsg>(OnViewFireMission);
                subs.Event<DropshipTerminalWeaponsEditFireMissionMsg>(OnEditFireMission);
                subs.Event<DropshipTerminalWeaponsDeleteFireMissionMsg>(OnDeleteFireMission);
                subs.Event<DropshipTerminalWeaponsSelectFireMissionMsg>(OnSelectFireMission);
                subs.Event<DropshipTerminalWeaponsSelectStrikeVectorMsg>(OnSelectStrikeVector);
                subs.Event<DropshipTerminalWeaponsFireMissionNextMsg>(OnNextFireMission);
                subs.Event<DropshipTerminalWeaponsFireMissionPreviousMsg>(OnPreviousFireMission);
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

        if (HasComp<PhysicsComponent>(ent))
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
                TrySetCameraTarget(uid, null);
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
            if (HasComp<RMCEquipmentDeployerComponent>(weapon))
            {
                var point = Transform(GetEntity(args.Weapon)).ParentUid;
                SetScreenUtility<RMCEquipmentDeployerComponent>(ent, args.First, EquipmentDeployer, GetNetEntity(point));
            }
            return;
        }

        ref var screen = ref args.First ? ref ent.Comp.ScreenOne : ref ent.Comp.ScreenTwo;
        screen.Weapon = args.Weapon;
        screen.FireMissionId = null;

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

    private void OnWeaponsChooseSpotlightMsg(Entity<DropshipTerminalWeaponsComponent> ent, ref DropshipTerminalWeaponsChooseSpotlightMsg args)
    {
        SetScreenUtility<DropshipSpotlightComponent>(ent, args.First, Spotlight, args.Slot);
    }

    private void OnWeaponsChooseEquipmentDeployerMsg(Entity<DropshipTerminalWeaponsComponent> ent, ref DropshipTerminalWeaponsChooseEquipmentDeployerMsg args)
    {
        SetScreenUtility<RMCEquipmentDeployerComponent>(ent, args.First, EquipmentDeployer, args.Slot);
    }

    private void OnWeaponsFireMsg(Entity<DropshipTerminalWeaponsComponent> ent, ref DropshipTerminalWeaponsFireMsg args)
    {
        if (_net.IsClient)
            return;

        var actor = args.Actor;
        ref var screen = ref args.First ? ref ent.Comp.ScreenOne : ref ent.Comp.ScreenTwo;

        // Try to start a fire mission if one is selected.
        if (screen.FireMissionId is { } missionId)
        {
            if (!_dropship.TryGetGridDropship(ent, out var dropship))
                return;

            if (ent.Comp.Target == null)
                return;

            if (!IsValidTarget(ent.Comp.Target.Value))
            {
                RemovePvsActors(ent);
                SetTarget(ent, null);
                Dirty(ent);
                return;
            }

            foreach (var fireMission in ent.Comp.FireMissions)
            {
                if (fireMission.Id != missionId)
                    continue;

                TryStartFireMission(dropship, ent.Comp.Target.Value, ent.Comp.StrikeVector, ent.Comp.MaxTiming, ent.Comp.Offset, args.Actor, fireMission, ent);
                break;
            }
            return;
        }

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

        var time = _timing.CurTime;
        if (time < weaponComp.NextFireAt)
        {
            var msg = Loc.GetString("rmc-dropship-weapons-fire-cooldown", ("weapon", weapon));
            _popup.PopupCursor(msg, actor);
            return;
        }

        TryFireWeapon(weapon.Value, target.ToCoordinates(), DropshipWeaponStrikeType.Direct, args.Actor, terminalComp: ent.Comp, weaponComp: weaponComp);
    }

    private void OnDropshipWeaponOnSpawnFire(Entity<ActivateDropshipWeaponOnSpawnComponent> active, ref MapInitEvent args)
    {
        if (_net.IsClient || !TryComp<DropshipAmmoComponent>(active, out var ammo))
            return;

        var time = _timing.CurTime;

        var inFlight = Spawn(null, MapCoordinates.Nullspace);
        var inFlightComp = new AmmoInFlightComponent
        {
            Target = _transform.GetMoverCoordinates(active).SnapToGrid(EntityManager, _mapManager),
            MarkerAt = time + ammo.TravelTime,
            ShotsLeft = ammo.RoundsPerShot,
            ShotsPerVolley = ammo.ShotsPerVolley,
            Damage = ammo.Damage,
            ArmorPiercing = ammo.ArmorPiercing,
            BulletSpread = ammo.BulletSpread,
            SoundTravelTime = ammo.SoundTravelTime,
            SoundMarker = ammo.SoundMarker,
            SoundGround = ammo.SoundGround,
            SoundImpact = ammo.SoundImpact,
            SoundWarning = ammo.SoundWarning,
            MarkerWarning = ammo.MarkerWarning,
            WarningMarkerAt = time + ammo.MarkerDuration,
            ImpactEffects = ammo.ImpactEffects,
            Explosion = ammo.Explosion,
            Implosion = ammo.Implosion,
            Fire = ammo.Fire,
            SoundEveryShots = ammo.SoundEveryShots,
        };

        AddComp(inFlight, inFlightComp, true);
        QueueDel(active);
    }

    private void OnWeaponsNightVisionMsg(Entity<DropshipTerminalWeaponsComponent> ent, ref DropshipTerminalWeaponsNightVisionMsg args)
    {
        if (_net.IsClient)
            return;

        ent.Comp.NightVision = args.On;
        if (EnsureTargetEye(ent, ent.Comp.CameraTarget) is { } target)
            _eye.SetDrawLight(target, !ent.Comp.NightVision);
        else if (HasComp<EyeComponent>(ent.Comp.CameraTarget))
        {
            _eye.SetDrawLight(ent.Comp.CameraTarget.Value, !ent.Comp.NightVision);
        }
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
            Strike or StrikeWeapon or StrikeFireMission or StrikeVector => Target,
            _ => screen.State,
        };

        Dirty(ent);
        RefreshWeaponsUI(ent);
    }

    private void OnWeaponsAdjustOffset(Entity<DropshipTerminalWeaponsComponent> ent, ref DropshipTerminalWeaponsAdjustOffsetMsg args)
    {
        if (!args.Direction.IsCardinal())
            return;

        if (_dropship.TryGetGridDropship(ent, out var dropship) &&
            _fireMission.HasActiveFireMission(dropship))
            return;

        var adjust = args.Direction.ToIntVec();
        var newOffset = ent.Comp.Offset + adjust;
        var limit = ent.Comp.OffsetLimit;
        newOffset = new Vector2i(
            Math.Clamp(newOffset.X, -limit.X, limit.X),
            Math.Clamp(newOffset.Y, -limit.Y, limit.Y)
        );

        ent.Comp.Offset = newOffset;

        if (EnsureTargetEye(ent, ent.Comp.CameraTarget) is { } target)
            _eye.SetOffset(target, ent.Comp.Offset);

        Dirty(ent);
        RefreshWeaponsUI(ent);
    }

    private void OnWeaponsResetOffset(Entity<DropshipTerminalWeaponsComponent> ent, ref DropshipTerminalWeaponsResetOffsetMsg args)
    {
        if (_dropship.TryGetGridDropship(ent, out var dropship) &&
            _fireMission.HasActiveFireMission(dropship))
            return;

        ent.Comp.Offset = Vector2i.Zero;

        if (EnsureTargetEye(ent, ent.Comp.CameraTarget) is { } target)
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
            if (!_area.CanCAS(coordinates))
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
                _door.TrySetBoltDown((child, doorBolt), true);
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

    private void OnWeaponsSpotlightSelect(Entity<DropshipTerminalWeaponsComponent> ent,
        ref DropShipTerminalWeaponsSpotlightToggleMsg args)
    {
        var selectedSystem = GetEntity(ent.Comp.SelectedSystem);
        if (!_dropship.TryGetGridDropship(ent, out var dropship) ||
            dropship.Comp.AttachmentPoints.Count == 0)
            return;

        if (!TryComp(selectedSystem, out DropshipSpotlightComponent? spotlight))
            return;

        var systemPoint = Transform(selectedSystem.Value).ParentUid;
        _pointLight.SetEnabled(systemPoint, args.On);
        _appearance.SetData(systemPoint, DropshipUtilityVisuals.State, args.On ? SpotlightState + "on" : SpotlightState + "off");
        spotlight.Enabled = args.On;

        Dirty(ent);
        RefreshWeaponsUI(ent);
    }

    private void OnEquipmentDeploy(Entity<DropshipTerminalWeaponsComponent> ent,
        ref DropShipTerminalWeaponsEquipmentDeployToggleMsg args)
    {
        var selectedSystem = GetEntity(ent.Comp.SelectedSystem);
        if (!_dropship.TryGetGridDropship(ent, out var dropship) ||
            dropship.Comp.AttachmentPoints.Count == 0)
            return;

        if (selectedSystem == null || !_equipmentDeployer.TryGetContainer(selectedSystem.Value, out var container))
            return;

        var deployOffset = new Vector2();
        var rotationOffset = 0f;
        if (TryComp(selectedSystem, out DropshipWeaponPointComponent? weaponPoint))
        {
            _equipmentDeployer.TryGetOffset(container.ContainedEntities[0],
                out deployOffset,
                out rotationOffset,
                weaponPoint.Location);
        }

        _equipmentDeployer.TryDeploy(container.ContainedEntities[0], args.Deploy, deployOffset, rotationOffset, user: args.Actor);
        RefreshWeaponsUI(ent);
    }

    private void OnEquipmentToggleAutoDeploy(Entity<DropshipTerminalWeaponsComponent> ent,
        ref DropShipTerminalWeaponsEquipmentAutoDeployToggleMsg args)
    {
        var selectedSystem = GetEntity(ent.Comp.SelectedSystem);
        if (!_dropship.TryGetGridDropship(ent, out var dropship) ||
            dropship.Comp.AttachmentPoints.Count == 0)
            return;

        if (selectedSystem == null || !_equipmentDeployer.TryGetContainer(selectedSystem.Value, out var container))
            return;

        _equipmentDeployer.SetAutoDeploy(container.ContainedEntities[0], args.AutoDeploy);
        RefreshWeaponsUI(ent);
    }

    private void OnCreateFireMission(Entity<DropshipTerminalWeaponsComponent> ent, ref DropshipTerminalWeaponsCreateFireMissionMsg args)
    {
        if (string.IsNullOrWhiteSpace(args.Name))
            return;

        if (!_dropship.TryGetGridDropship(ent, out var dropship))
            return;

        foreach (var mission in ent.Comp.FireMissions)
        {
            if (mission.Name == args.Name)
                return;
        }

        // Generate a new unique FireMission Id
        var newId = ent.Comp.FireMissions.Count > 0
            ? ent.Comp.FireMissions.Max(fm => fm.Id) + 1
            : 1;

        var weaponOffsets = new List<WeaponOffsetData>();
        var weapons = new List<EntityUid>();
        foreach (var attachmentPoint in dropship.Comp.AttachmentPoints)
        {
            if (!TryComp(attachmentPoint, out DropshipWeaponPointComponent? weaponPoint) ||
                !_container.TryGetContainer(attachmentPoint, weaponPoint.WeaponContainerSlotId, out var weaponContainer) ||
                weaponContainer.ContainedEntities.Count == 0)
                continue;

            weapons.Add(weaponContainer.ContainedEntities[0]);
        }

        foreach (var weapon in weapons)
        {
            for (var timing = ent.Comp.MinTiming; timing <= ent.Comp.MaxTiming; timing++)
            {
                weaponOffsets.Add(new WeaponOffsetData(GetNetEntity(weapon), timing, null));
            }
        }

        var cutName = args.Name[..Math.Min(ent.Comp.MaxFireMissionNameLength, args.Name.Length)];
        var newFireMission = new FireMissionData(newId, cutName, weaponOffsets);
        ent.Comp.FireMissions.Add(newFireMission);
        Dirty(ent);

        RefreshWeaponsUI(ent);
    }

    private void OnViewFireMission(Entity<DropshipTerminalWeaponsComponent> ent, ref DropshipTerminalWeaponsViewFireMissionMsg args)
    {
        var idToSelect = args.Id;
        if (ent.Comp.FireMissions.All(fm => fm.Id != idToSelect))
            return;

        if (args.First)
            ent.Comp.ScreenOneViewingFireMissionId = idToSelect;
        else
            ent.Comp.ScreenTwoViewingFireMissionId = idToSelect;

        ref var screen = ref args.First ? ref ent.Comp.ScreenOne : ref ent.Comp.ScreenTwo;
        screen.State = FireMissionView;

        Dirty(ent);
        RefreshWeaponsUI(ent);
    }

    private void OnEditFireMission(Entity<DropshipTerminalWeaponsComponent> ent, ref DropshipTerminalWeaponsEditFireMissionMsg args)
    {

        var idToSelect = args.MissionId;
        var mission = ent.Comp.FireMissions.FirstOrDefault(fm => fm.Id == idToSelect);
        ref var screen = ref args.First ? ref ent.Comp.ScreenOne : ref ent.Comp.ScreenTwo;

        if (screen.Weapon == null)
            return;

        var offsets = mission.WeaponOffsets;

        // Look for an existing offset for this weapon and step
        var existingIndex = -1;
        for (var i = 0; i < offsets.Count; i++)
        {
            if (offsets[i].WeaponId != screen.Weapon || offsets[i].Step != args.Step)
                continue;

            existingIndex = i;
            break;
        }

        if (!IsOffsetEditValid(ent, idToSelect, screen.Weapon.Value, args.Step, args.Offset, args.Actor, out var suspicious))
        {
            if (suspicious && _player.TryGetSessionByEntity(args.Actor, out var player))
                _chat.SendAdminAlert(Loc.GetString("rmc-dropship-firemission-invalid-value-admin-announcement", ("player", player)));
            return;
        }

        var newOffset = new WeaponOffsetData(
            screen.Weapon.Value,
            args.Step,
            args.Offset
        );

        if (existingIndex != -1)
            offsets[existingIndex] = newOffset;
        else
            offsets.Add(newOffset);

        Dirty(ent);
        RefreshWeaponsUI(ent);
    }

    private void OnDeleteFireMission(Entity<DropshipTerminalWeaponsComponent> ent, ref DropshipTerminalWeaponsDeleteFireMissionMsg args)
    {
        ref var currentScreen = ref args.First ? ref ent.Comp.ScreenOne : ref ent.Comp.ScreenTwo;
        ref var otherScreen = ref args.First ? ref ent.Comp.ScreenTwo : ref ent.Comp.ScreenOne;

        var idToDelete = args.First
            ? ent.Comp.ScreenOneViewingFireMissionId
            : ent.Comp.ScreenTwoViewingFireMissionId;

        if (idToDelete == null)
            return;

        var missionId = idToDelete.Value;
        ent.Comp.FireMissions.RemoveAll(fm => fm.Id == missionId);

        // Reset the current screen
        currentScreen.State = FireMissionCreate;
        if (args.First)
            ent.Comp.ScreenOneViewingFireMissionId = null;
        else
            ent.Comp.ScreenTwoViewingFireMissionId = null;

        // Reset the other screen if it was viewing or editing the deleted mission
        if (otherScreen.State is FireMissionEdit or FireMissionView)
        {
            var otherSelectedId = args.First
                ? ent.Comp.ScreenTwoViewingFireMissionId
                : ent.Comp.ScreenOneViewingFireMissionId;

            if (otherSelectedId == missionId)
            {
                otherScreen.State = FireMissionCreate;
                if (args.First)
                    ent.Comp.ScreenTwoViewingFireMissionId = null;
                else
                    ent.Comp.ScreenOneViewingFireMissionId = null;
            }
        }

        Dirty(ent);
        RefreshWeaponsUI(ent);
    }

    private void OnSelectFireMission(Entity<DropshipTerminalWeaponsComponent> ent, ref DropshipTerminalWeaponsSelectFireMissionMsg args)
    {
        ref var screen = ref args.First ? ref ent.Comp.ScreenOne : ref ent.Comp.ScreenTwo;
        screen.FireMissionId = args.Id;
        screen.Weapon = null;
        screen.State = Target;

        Dirty(ent);
        RefreshWeaponsUI(ent);
    }

    private void OnSelectStrikeVector(Entity<DropshipTerminalWeaponsComponent> ent, ref DropshipTerminalWeaponsSelectStrikeVectorMsg args)
    {
        if (args.Direction != Direction.North &&
            args.Direction != Direction.East &&
            args.Direction != Direction.South &&
            args.Direction != Direction.West)
            return;

        ent.Comp.StrikeVector = args.Direction;
        ref var screen = ref args.First ? ref ent.Comp.ScreenOne : ref ent.Comp.ScreenTwo;
        screen.State = Target;

        Dirty(ent);
        RefreshWeaponsUI(ent);
    }

    private void OnNextFireMission(Entity<DropshipTerminalWeaponsComponent> ent, ref DropshipTerminalWeaponsFireMissionNextMsg args)
    {
        ref var page = ref args.First ? ref ent.Comp.ScreenOneFireMissionPage : ref ent.Comp.ScreenTwoFireMissionPage;
        var itemsPerPage = args.TargetsPerPage - args.FixedButtonsCount;
        var totalPages = (ent.Comp.FireMissions.Count + itemsPerPage - 1) / itemsPerPage;
        var maxPageIndex = totalPages - 1;

        page = Math.Min(page + 1, maxPageIndex);

        Dirty(ent);
        RefreshWeaponsUI(ent);
    }

    private void OnPreviousFireMission(Entity<DropshipTerminalWeaponsComponent> ent, ref DropshipTerminalWeaponsFireMissionPreviousMsg args)
    {
        ref var page = ref args.First ? ref ent.Comp.ScreenOneFireMissionPage : ref ent.Comp.ScreenTwoFireMissionPage;
        var itemsPerPage = args.TargetsPerPage - args.FixedButtonsCount;
        var totalPages = (ent.Comp.FireMissions.Count + itemsPerPage - 1) / itemsPerPage;
        var maxPageIndex = totalPages - 1;

        page = Math.Min(page - 1, maxPageIndex);

        Dirty(ent);
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
        SetTarget(ent, target);
        if (TryUpdateCameraTarget(ent, target, terminalComp: ent.Comp))
            return;

        RefreshWeaponsUI(ent);
        Dirty(ent);
    }

    public bool TryUpdateCameraTarget(EntityUid terminal, EntityUid target, bool force = false, DropshipTerminalWeaponsComponent? terminalComp = null)
    {
        if (!Resolve(terminal, ref terminalComp, false))
            return false;

        if (!_dropship.TryGetGridDropship(terminal, out var dropship))
            return false;

        if (!force && _fireMission.HasActiveFireMission(dropship))
            return false;

        RemovePvsActors((terminal, terminalComp));
        TrySetCameraTarget(terminal, target, terminalComp);

        if (EnsureTargetEye((terminal, terminalComp), terminalComp.CameraTarget) is { } targetEye)
        {
            _eye.SetOffset(targetEye, terminalComp.Offset);
            _eye.SetDrawLight(targetEye, !terminalComp.NightVision);
        }
        else if (HasComp<EyeComponent>(target))
        {
            _eye.SetDrawLight(target, !terminalComp.NightVision);

            var targetEyeComp = EnsureComp<DropshipTargetEyeComponent>(target);
            targetEyeComp.Target = target;
            Dirty(target, targetEyeComp);
        }

        AddPvsActors((terminal, terminalComp));
        RefreshWeaponsUI((terminal, terminalComp));
        Dirty(terminal, terminalComp);

        return true;
    }

    private bool IsOffsetEditValid(Entity<DropshipTerminalWeaponsComponent> ent, int fireMissionId, NetEntity weapon, int step, int? offset, EntityUid user, out bool suspicious)
    {
        suspicious = false;
        var weaponEntity = GetEntity(weapon);

        // Validate fire mission exists.
        FireMissionData? fireMission = null;
        foreach (var mission in ent.Comp.FireMissions)
        {
            if (mission.Id != fireMissionId)
                continue;

            fireMission = mission;
            break;
        }

        if (fireMission == null)
            return false;

        var weaponOffsetsForWeapon = new List<WeaponOffsetData>();
        foreach (var wo in fireMission.Value.WeaponOffsets)
        {
            if (wo.WeaponId == weapon)
                weaponOffsetsForWeapon.Add(wo);
        }

        // Check if the timing is allowed.
        if (step < ent.Comp.MinTiming || step > ent.Comp.MaxTiming)
        {
            suspicious = true;
            return false;
        }

        // Check if the weapon point has a location set.
        if (!TryGetWeaponLocation(weaponEntity, out var location))
            return false;

        // Check if the terminal has allowed offsets defined for this location.
        if (!ent.Comp.AllowedOffsets.TryGetValue(location, out var validOffsets))
            return false;

        // Check if the offset is allowed
        if (!validOffsets.Contains(offset))
        {
            // This means the client sent an offset value that should be impossible to be set through normal gameplay.
            suspicious = true;
            return false;
        }

        if (!TryComp(weaponEntity, out DropshipWeaponComponent? weaponComp))
            return false;

        // Check if a weapons has ammo loaded that can't be used during a fire mission.
        if (TryGetWeaponAmmo((weaponEntity, weaponComp), out var ammo) &&
            ammo.Comp.FireMissionDelay == null)
        {
            var msg = Loc.GetString("rmc-dropship-firemission-invalid-ammo");
            _popup.PopupCursor(msg, user, PopupType.SmallCaution);
            return false;
        }

        // Check if the step is blocked by an already set offset in a nearby step.
        foreach (var existing in weaponOffsetsForWeapon)
        {
            if (!existing.Offset.HasValue)
                continue;

            if (existing.Step == step)
                continue;

            if (Math.Abs(existing.Step - step) <= ammo.Comp.FireMissionDelay)
                return false;
        }

        return true;
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

            if (!flight.WarnedSound)
            {
                flight.WarnedSound = true;
                Dirty(uid, flight);

                if (flight.SoundWarning != null)
                    _audio.PlayPvs(flight.SoundWarning, flight.Target);
            }

            if (time >= flight.WarningMarkerAt && !flight.WarnedMarker)
            {
                flight.WarnedMarker = true;
                Dirty(uid, flight);

                if (flight.MarkerWarning)
                {
                    flight.WarningMarker = Spawn(DropshipTargetMarker, flight.Target);
                    var despawn = EnsureComp<TimedDespawnComponent>(flight.WarningMarker.Value);
                    despawn.Lifetime = (float)(flight.MarkerAt - _timing.CurTime).TotalSeconds;
                }
            }

            if (flight.MarkerAt > time)
                continue;

            if (!flight.SpawnedMarker)
            {
                flight.SpawnedMarker = true;
                flight.Marker = Spawn(DropshipTargetMarker, flight.Target);
                Dirty(uid, flight);

                _audio.PlayPvs(flight.SoundMarker, flight.Marker.Value);

                if (_net.IsServer)
                    QueueDel(flight.WarningMarker);

                flight.WarningMarker = null;
            }

            if (flight.MarkerAt.Add(flight.MarkerDuration) > time)
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
                foreach (var effect in flight.ImpactEffects)
                {
                    Spawn(effect, target, rotation: _random.NextAngle());
                }

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

                if (flight.Implosion != null)
                {
                    _rmcImplosion.Implode(flight.Implosion, target);
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
                        _rmcFlammable.SpawnFireLines(flight.Fire.Type,
                            flight.Target,
                            flight.Fire.CardinalRange,
                            flight.Fire.OrdinalRange,
                            flight.Fire.Intensity,
                            flight.Fire.Duration);

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

                if (flight.Explosion != null)
                {
                    _rmcExplosion.QueueExplosion(target,
                        flight.Explosion.Type,
                        flight.Explosion.Total,
                        flight.Explosion.Slope,
                        flight.Explosion.Max,
                        uid,
                        canCreateVacuum: false
                    );
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
        if (EntityManager.HasComponent<EyeComponent>(target))
        {
            eye = target;
            return true;
        }

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

    private void SetScreenUtility<T>(Entity<DropshipTerminalWeaponsComponent> ent, bool first, DropshipTerminalWeaponsScreen state, NetEntity? selected = null) where T : IComponent
    {
        if (!_dropship.TryGetGridDropship(ent, out var dropship) ||
            dropship.Comp.AttachmentPoints.Count == 0)
        {
            return;
        }

        var hasUtility = false;
        var hasElectronicSystem = false;
        var hasWeaponSystem = false;
        foreach (var point in dropship.Comp.AttachmentPoints)
        {
            if (TryComp(point, out DropshipElectronicSystemPointComponent? electronicSystemComp) &&
                _container.TryGetContainer(point, electronicSystemComp.ContainerId, out var electronicSystemContainer) &&
                electronicSystemContainer.ContainedEntities.Count > 0)
            {
                foreach (var contained in electronicSystemContainer.ContainedEntities)
                {
                    if (!HasComp<T>(contained))
                        continue;

                    hasElectronicSystem = true;
                    break;
                }
            }

            if (TryComp(point, out DropshipWeaponPointComponent? weaponPointComponent) &&
                _container.TryGetContainer(point, weaponPointComponent.WeaponContainerSlotId, out var weaponContainer) &&
                weaponContainer.ContainedEntities.Count > 0)
            {
                foreach (var contained in weaponContainer.ContainedEntities)
                {
                    if (!HasComp<T>(contained))
                        continue;

                    hasWeaponSystem = true;
                    break;
                }
            }

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

        if (!hasUtility && !hasElectronicSystem && !hasWeaponSystem)
            return;

        ref var screen = ref first ? ref ent.Comp.ScreenOne : ref ent.Comp.ScreenTwo;
        screen.State = state;
        ent.Comp.SelectedSystem = selected;

        Dirty(ent);
        RefreshWeaponsUI(ent);
    }

    /// <summary>
    ///     Tries to get the <see cref="DropshipWeaponPointLocation"/> of the weapon point the weapon is installed in.
    /// </summary>
    /// <param name="weaponEntity">The entity to check the location of</param>
    /// <param name="locationId">The location of the weapon point</param>
    /// <returns>True if a location was found</returns>
    public bool TryGetWeaponLocation(EntityUid weaponEntity, out DropshipWeaponPointLocation locationId)
    {
        locationId = default;

        if (!_container.TryGetContainingContainer(weaponEntity, out var container))
            return false;

        if (!TryComp(container.Owner, out DropshipWeaponPointComponent? point))
            return false;

        if (point.Location is not { } location)
            return false;

        locationId = location;
        return true;
    }

    /// <summary>
    ///     Makes sure an offset exists for the given weapon in the given fire mission.
    /// </summary>
    /// <param name="uid">The weapons control entity</param>
    /// <param name="fireMission">The fire mission data</param>
    /// <param name="weaponEntity">The weapon entity</param>
    /// <param name="terminal">The <see cref="DropshipTerminalWeaponsComponent"/> of given uid entity</param>
    public void EnsureWeaponOffsets(EntityUid uid, FireMissionData fireMission, EntityUid weaponEntity, DropshipTerminalWeaponsComponent? terminal = null)
    {
        if (!Resolve(uid, ref terminal, false))
            return;

        var existingOffsets = fireMission.WeaponOffsets
            .Where(o => o.WeaponId == GetNetEntity(weaponEntity))
            .ToList();

        if (existingOffsets.Count == terminal.MaxTiming)
            return;

        var missingSteps = Enumerable.Range(terminal.MinTiming, terminal.MaxTiming)
            .Where(step => existingOffsets.All(o => o.Step != step))
            .ToList();

        foreach (var step in missingSteps)
        {
            var newOffset = new WeaponOffsetData(GetNetEntity(weaponEntity), step, null);
            fireMission.WeaponOffsets.Add(newOffset);
        }

        Dirty(uid, terminal);
        RefreshWeaponsUI((uid, terminal));
    }

    /// <summary>
    ///     Attempt to start a fire mission at the target's location.
    /// </summary>
    /// <param name="dropship">The dropship entity performing the fire mission</param>
    /// <param name="target">The target of the fire mission</param>
    /// <param name="strikeVector">The direction the fire mission will move towards</param>
    /// <param name="maxSteps">The maximum duration of the mission</param>
    /// <param name="offset">The offset that will be applied to the target's location to determine the start location of the mission</param>
    /// <param name="user">The entity that tried to start the fire mission</param>
    /// <param name="missionData">The fire mission data, this determines when and where to shoot</param>
    /// <param name="watchingTerminal">The terminal used to start the mission</param>
    /// <returns>True if a fire mission was successfully started</returns>
    public bool TryStartFireMission(Entity<DropshipComponent> dropship, EntityUid target, Direction strikeVector, int maxSteps, Vector2 offset, EntityUid user, FireMissionData missionData, EntityUid? watchingTerminal = null)
    {
        if (HasComp<ActiveFireMissionComponent>(dropship))
        {
            var cooldownMsg = Loc.GetString("rmc-dropship-firemission-cooldown");
            _popup.PopupCursor(cooldownMsg, user, PopupType.SmallCaution);
            return false;
        }

        var shotsPerWeapon = new Dictionary<EntityUid, int>();
        foreach (var weaponOffset in missionData.WeaponOffsets)
        {
            if (weaponOffset.Offset == null)
                continue;

            var weapon = GetEntity(weaponOffset.WeaponId);

            // Ignore weapons not mounted on the dropship that is performing the mission.
            if (!TryGetWeaponLocation(weapon, out _) ||
                !_dropship.TryGetGridDropship(weapon, out var weaponDropship)
                || weaponDropship != dropship)
                continue;

            if (!shotsPerWeapon.TryAdd(weapon, 1))
                shotsPerWeapon[weapon]++;
        }

        // Don't start the mission if at least one weapon can't shoot.
        foreach (var (weapon, shotCount) in shotsPerWeapon)
        {
            if (!CanFire(weapon, DropshipWeaponStrikeType.FireMission, user, shotCount))
                return false;
        }

        var targetEntity = Spawn(null, _transform.GetMapCoordinates(target));
        var missionEye = Spawn(null, _transform.GetMapCoordinates(target).Offset(offset));
        var eyeComp = EnsureComp<EyeComponent>(missionEye);
        _eye.SetDrawFov(missionEye, false, eyeComp);

        var activeFireMission = EnsureComp<ActiveFireMissionComponent>(dropship);
        activeFireMission.StartTime = _timing.CurTime;
        activeFireMission.TargetCoordinates = targetEntity.ToCoordinates();
        activeFireMission.MaxSteps = maxSteps;
        activeFireMission.StrikeVector = strikeVector;
        activeFireMission.Offset = offset;
        activeFireMission.MissionEye = missionEye;
        activeFireMission.WatchingTerminal = watchingTerminal;
        activeFireMission.FireMissionData = missionData;
        Dirty(dropship, activeFireMission);

        var msg = Loc.GetString("rmc-dropship-firemission-started");
        _popup.PopupCursor(msg, user, PopupType.SmallCaution);

        return true;
    }

    /// <summary>
    ///     Try to fire the dropship weapon at the targeted location.
    /// </summary>
    /// <param name="weapon">The weapon being shot</param>
    /// <param name="targetCoordinates">The target coordinates</param>
    /// <param name="strikeType">The <see cref="DropshipWeaponStrikeType"/></param>
    /// <param name="actor">The entity trying to shoot the weapon</param>
    /// <param name="terminalComp">The terminal used to shoot the weapon</param>
    /// <param name="weaponComp">The <see cref="DropshipWeaponComponent"/> of the shooting weapon</param>
    /// <returns>True if the weapon was fired</returns>
    public bool TryFireWeapon(EntityUid weapon, EntityCoordinates targetCoordinates, DropshipWeaponStrikeType strikeType, EntityUid? actor = null, DropshipTerminalWeaponsComponent? terminalComp = null, DropshipWeaponComponent? weaponComp = null)
    {
        if (!_dropship.TryGetGridDropship(weapon, out var dropship))
            return false;

        if (!Resolve(weapon, ref weaponComp, false))
            return false;

        if (!CanFire(weapon, strikeType, actor, weapon: weaponComp))
            return false;

        FireWeapon(weapon, targetCoordinates, strikeType,dropship, actor, terminalComp, weaponComp);
        return true;
    }

    /// <summary>
    ///     Checks if the weapon is able to fire.
    /// </summary>
    /// <param name="uid">The weapon to check.</param>
    /// <param name="strikeType">The method through which the weapon is attempted to be fired</param>
    /// <param name="actor">The entity trying to fire the weapon</param>
    /// <param name="requiredShots">The amount of times the weapon should be able to shoot</param>
    /// <param name="weapon">The <see cref="DropshipWeaponComponent"/> of the shooting weapon</param>
    /// <returns>True if the weapon is able to shoot</returns>
    private bool CanFire(EntityUid uid, DropshipWeaponStrikeType strikeType, EntityUid? actor = null, int requiredShots = 1, DropshipWeaponComponent? weapon = null)
    {
        if (!Resolve(uid, ref weapon, false))
            return false;

        Entity<DropshipComponent> dropship = default;
        if (!CasDebug)
        {
            if (!_dropship.TryGetGridDropship(uid, out dropship))
                return false;

            if (!TryComp(dropship, out FTLComponent? ftl) ||
                (ftl.State != FTLState.Travelling && ftl.State != FTLState.Arriving))
            {
                if (actor == null)
                    return false;

                var msg = Loc.GetString("rmc-dropship-weapons-fire-not-flying");
                _popup.PopupCursor(msg, actor.Value, PopupType.SmallCaution);

                return false;
            }
        }

        if (_fireMission.HasActiveFireMission(dropship) && strikeType != DropshipWeaponStrikeType.FireMission)
                return false;

        if (!CasDebug &&
            !weapon.FireInTransport &&
            !HasComp<DropshipInFlyByComponent>(dropship))
        {
            // TODO RMC14 fire mission only weapons
            return false;
        }

        if (!TryGetWeaponAmmo((uid, weapon), out var ammo) ||
            ammo.Comp.Rounds < ammo.Comp.RoundsPerShot * requiredShots)
        {
            if (actor == null)
                return false;

            var msg = Loc.GetString("rmc-dropship-weapons-fire-no-ammo", ("weapon", uid));
            _popup.PopupCursor(msg, actor.Value);
            return false;
        }

        if (strikeType == DropshipWeaponStrikeType.FireMission && ammo.Comp.FireMissionDelay == null)
        {
            if (actor == null)
                return false;

            var msg = Loc.GetString("rmc-dropship-firemission-invalid-ammo", ("ammo", ammo));
            _popup.PopupCursor(msg, actor.Value, PopupType.SmallCaution);

            return false;
        }

        return true;
    }

    public bool TrySetCameraTarget(EntityUid terminal, EntityUid? newTarget, DropshipTerminalWeaponsComponent? terminalComp = null)
    {
        if (!Resolve(terminal, ref terminalComp, false))
            return false;

        if (newTarget == terminalComp.CameraTarget)
            return false;

        terminalComp.Offset = Vector2i.Zero;
        terminalComp.CameraTarget = newTarget;
        Dirty(terminal, terminalComp);

        return true;
    }

    private void FireWeapon(EntityUid weapon, EntityCoordinates targetCoordinates, DropshipWeaponStrikeType strikeType, EntityUid dropship,  EntityUid? actor = null, DropshipTerminalWeaponsComponent? terminalComp = null, DropshipWeaponComponent? weaponComp = null)
    {
        if (!Resolve(weapon, ref weaponComp, false))
            return;

        if (!TryGetWeaponAmmo(weapon, out var ammo))
            return;

        var time = _timing.CurTime;
        var travelTime = strikeType == DropshipWeaponStrikeType.FireMission ? TimeSpan.Zero : ammo.Comp.TravelTime;
        var targetSpread = strikeType == DropshipWeaponStrikeType.FireMission ? ammo.Comp.TargetSpread / 2 : ammo.Comp.TargetSpread;
        var markerDuration = strikeType == DropshipWeaponStrikeType.FireMission ? TimeSpan.Zero : TimeSpan.FromSeconds(DefaultMarkerDuration);
        var groundSound = strikeType == DropshipWeaponStrikeType.FireMission ? ammo.Comp.SoundCockpit : ammo.Comp.SoundGround;
        var soundTravelTime = strikeType == DropshipWeaponStrikeType.FireMission ? TimeSpan.Zero : ammo.Comp.SoundTravelTime;

        var ev = new DropshipWeaponShotEvent(
            targetSpread,
            ammo.Comp.BulletSpread,
            travelTime,
            ammo.Comp.RoundsPerShot,
            ammo.Comp.ShotsPerVolley,
            ammo.Comp.Damage,
            ammo.Comp.ArmorPiercing,
            soundTravelTime,
            ammo.Comp.SoundCockpit,
            ammo.Comp.SoundMarker,
            groundSound,
            ammo.Comp.SoundImpact,
            ammo.Comp.SoundWarning,
            ammo.Comp.MarkerWarning,
            ammo.Comp.ImpactEffects,
            ammo.Comp.Explosion,
            ammo.Comp.Implosion,
            ammo.Comp.Fire,
            ammo.Comp.SoundEveryShots
        );
        RaiseLocalEvent(dropship, ref ev);

        ammo.Comp.Rounds -= ammo.Comp.RoundsPerShot;
        _appearance.SetData(ammo, DropshipAmmoVisuals.Fill, ammo.Comp.Rounds);
        _powerloader.SyncAppearance(Transform(weapon).ParentUid);
        Dirty(ammo);

        _audio.PlayPvs(ev.SoundCockpit, weapon);
        weaponComp.NextFireAt = time + weaponComp.FireDelay;
        Dirty(weapon, weaponComp);

        var spread = ev.Spread;
        var targetCoords = targetCoordinates;
        if (spread != 0)
            targetCoords = targetCoords.Offset(_random.NextVector2(-spread, spread + 1));

        var inFlight = Spawn(null, MapCoordinates.Nullspace);
        var inFlightComp = new AmmoInFlightComponent
        {
            Target = targetCoords,
            MarkerAt = time + ev.TravelTime,
            ShotsLeft = ev.RoundsPerShot,
            ShotsPerVolley = ev.ShotsPerVolley,
            Damage = ev.Damage,
            ArmorPiercing = ev.ArmorPiercing,
            BulletSpread = ev.BulletSpread,
            SoundTravelTime = ev.SoundTravelTime,
            SoundMarker = ev.SoundMarker,
            SoundGround = ev.SoundGround,
            SoundImpact = ev.SoundImpact,
            SoundWarning = ev.SoundWarning,
            MarkerWarning = ev.MarkerWarning,
            WarningMarkerAt = time + markerDuration,
            ImpactEffects = ev.ImpactEffect,
            Explosion = ev.Explosion,
            Implosion = ev.Implosion,
            Fire = ev.Fire,
            SoundEveryShots = ev.SoundEveryShots,
            MarkerDuration = markerDuration,
        };
        AddComp(inFlight, inFlightComp, true);

        if (ammo.Comp.DeleteOnEmpty && ammo.Comp.Rounds <= 0)
            QueueDel(ammo);

        if (!Resolve(weapon, ref weaponComp, false))
            return;

        if (terminalComp == null)
            return;

        _adminLog.Add(LogType.RMCDropshipWeapon,
            $"{ToPrettyString(actor)} fired {ToPrettyString(weapon)} at {ToPrettyString(terminalComp.Target)}");
    }
}

/// <summary>
///     Raised on a dropship when it shoots any of its weapons.
/// </summary>
[ByRefEvent]
public record struct DropshipWeaponShotEvent(
    float Spread,
    int BulletSpread,
    TimeSpan TravelTime,
    int RoundsPerShot,
    int ShotsPerVolley,
    DamageSpecifier? Damage,
    int ArmorPiercing,
    TimeSpan SoundTravelTime,
    SoundSpecifier? SoundCockpit,
    SoundSpecifier? SoundMarker,
    SoundSpecifier? SoundGround,
    SoundSpecifier? SoundImpact,
    SoundSpecifier? SoundWarning,
    bool MarkerWarning,
    List<EntProtoId> ImpactEffect,
    RMCExplosion? Explosion,
    RMCImplosion? Implosion,
    RMCFire? Fire,
    int SoundEveryShots
);
