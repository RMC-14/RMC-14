using Content.Server._RMC14.Dropship;
using Content.Server._RMC14.MapInsert;
using Content.Server._RMC14.Marines;
using Content.Server._RMC14.Power;
using Content.Server._RMC14.Stations;
using Content.Server._RMC14.Xenonids.Hive;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.Fax;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.Ghost;
using Content.Server.Mind;
using Content.Server.Players.PlayTimeTracking;
using Content.Server.Popups;
using Content.Server.Preferences.Managers;
using Content.Server.RoundEnd;
using Content.Server.Shuttles.Systems;
using Content.Server.Spawners.EntitySystems;
using Content.Server.Station.Systems;
using Content.Server.Stunnable;
using Content.Server.Voting;
using Content.Server.Voting.Managers;
using Content.Shared._RMC14.ARES;
using Content.Shared._RMC14.Armor.Ghillie;
using Content.Shared._RMC14.Armor.ThermalCloak;
using Content.Shared._RMC14.CameraShake;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.Intel;
using Content.Shared._RMC14.Item;
using Content.Shared._RMC14.Light;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.HyperSleep;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.Scaling;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Construction;
using Content.Shared._RMC14.Xenonids.Construction.Nest;
using Content.Shared._RMC14.Xenonids.Construction.Tunnel;
using Content.Shared._RMC14.Xenonids.Evolution;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Maturing;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.Actions;
using Content.Shared.Destructible;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Roles;
using Robust.Server.Audio;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager;

namespace Content.Server._RMC14.Rules.DistressSignal;

/// <summary>
/// Main game rule system for the distress signal round type.
/// Manages the complete round lifecycle including planet selection, player spawning,
/// hijack events, round end conditions, and faction balance between marines and xenos.
/// </summary>
public sealed partial class CMDistressSignalRuleSystem : GameRuleSystem<CMDistressSignalRuleComponent>
{
    /// <summary>
    /// Minimum number of living xenos required for a minor marine victory when the queen dies.
    /// </summary>
    private const int QueenDeathXenoThreshold = 4;

    [Dependency] private readonly FaxSystem _fax = default!;
    [Dependency] private readonly GunIFFSystem _gunIFF = default!;
    [Dependency] private readonly RMCPowerSystem _rmcPower = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly MapSystem _mapSystem = default!;
    [Dependency] private readonly ItemCamouflageSystem _camo = default!;
    [Dependency] private readonly XenoHiveSystem _hive = default!;
    [Dependency] private readonly XenoTunnelSystem _xenoTunnel = default!;
    [Dependency] private readonly ContainerSystem _containers = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly SharedDestructibleSystem _destruction = default!;
    [Dependency] private readonly RMCPlanetSystem _rmcPlanet = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly GhostSystem _ghost = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly XenoMaturingSystem _maturing = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly RMCCameraShakeSystem _rmcCameraShake = default!;
    [Dependency] private readonly StunSystem _stuns = default!;
    [Dependency] private readonly MapInsertSystem _mapInsert = default!;
    [Dependency] private readonly RMCAmbientLightSystem _rmcAmbientLight = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IVoteManager _voteManager = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly ARESSystem _ares = default!;
    [Dependency] private readonly MarineAnnounceSystem _marineAnnounce = default!;
    [Dependency] private readonly ThermalCloakSystem _thermalCloak = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedGhillieSuitSystem _ghillieSuit = default!;
    [Dependency] private readonly XenoEvolutionSystem _xenoEvolution = default!;
    [Dependency] private readonly RMCGameRuleExtrasSystem _gameRulesExtras = default!;
    [Dependency] private readonly IServerPreferencesManager _prefsManager = default!;
    [Dependency] private readonly IntelSystem _intel = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IBanManager _bans = default!;
    [Dependency] private readonly PlayTimeTrackingSystem _playTime = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;
    [Dependency] private readonly SharedXenoParasiteSystem _parasite = default!;
    [Dependency] private readonly RMCStationJobsSystem _rmcStationJobs = default!;
    [Dependency] private readonly SquadSystem _squad = default!;
    [Dependency] private readonly StationJobsSystem _stationJobs = default!;
    [Dependency] private readonly DropshipSystem _dropship = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly ScalingSystem _scaling = default!;
    [Dependency] private readonly SharedXenoConstructionSystem _xenoConstruction = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;

    private readonly HashSet<string> _operationNames = new();
    private readonly HashSet<string> _operationPrefixes = new();
    private readonly HashSet<string> _operationSuffixes = new();

    private float _marinesPerXeno;
    private bool _autoBalance;
    private float _autoBalanceStep;
    private float _autoBalanceMax;
    private float _autoBalanceMin;
    private float _marinesPerSurvivor;
    private float _maximumSurvivors;
    private float _minimumSurvivors;
    private int _mapVoteExcludeLast;
    private bool _useCarryoverVoting;
    private readonly TimeSpan _hijackStunTime = TimeSpan.FromSeconds(5);
    private bool _landingZoneMiasmaEnabled;
    private TimeSpan _sunsetDuration;
    private TimeSpan _sunriseDuration;
    private TimeSpan _forceEndHijackTime;
    private float _hijackShipWeight;
    private int _hijackMinBurrowed;
    private int _xenosMinimum;
    private bool _usingCustomOperationName;
    private bool _queenBuildingBoostEnabled;
    private TimeSpan _queenBoostDuration;
    private float _queenBoostSpeedMultiplier;
    private float _queenBoostRemoteRange;

    private bool _spawnedDropships;

    private readonly List<MapId> _almayerMaps = [];
    private readonly List<EntityUid> _marineList = [];

    private EntityQuery<HyperSleepChamberComponent> _hyperSleepChamberQuery;
    private EntityQuery<XenoNestedComponent> _xenoNestedQuery;

    private readonly Queue<EntProtoId<RMCPlanetMapPrototypeComponent>> _lastPlanetMaps = new();

    [ViewVariables]
    private RMCPlanet? SelectedPlanetMap { get; set; }

    [ViewVariables]
    public string? SelectedPlanetMapName => SelectedPlanetMap?.Proto.Name;

    [ViewVariables]
    public string? OperationName { get; private set; }

    public string? ActiveNightmareScenario { get; private set; }

    private readonly Dictionary<EntProtoId<RMCPlanetMapPrototypeComponent>, int> _carryoverVotes = new();

    private IVoteHandle? _currentVote;

    private Entity<CMDistressSignalRuleComponent>? _activeRule;

    private Entity<CMDistressSignalRuleComponent>? TryGetActiveRuleEntity()
    {
        if (_activeRule.HasValue && Exists(_activeRule.Value))
        {
            return _activeRule;
        }

        var query = QueryActiveRules();
        while (query.MoveNext(out var uid, out _, out var comp, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;

            _activeRule = (uid, comp);

            if (!Exists(_activeRule.Value))
            {
                _activeRule = null;
                return null;
            }

            return _activeRule;
        }
        return null;
    }

    private CMDistressSignalRuleComponent? TryGetActiveRule() => TryGetActiveRuleEntity()?.Comp;

    private void InvalidateActiveRule() => _activeRule = null;

    public override void Initialize()
    {
        base.Initialize();

        _hyperSleepChamberQuery = GetEntityQuery<HyperSleepChamberComponent>();
        _xenoNestedQuery = GetEntityQuery<XenoNestedComponent>();

        SubscribeLocalEvent<LoadingMapsEvent>(OnMapLoading);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        SubscribeLocalEvent<RulePlayerSpawningEvent>(OnRulePlayerSpawning);
        SubscribeLocalEvent<PlayerSpawningEvent>(OnPlayerSpawning,
             before: [typeof(ArrivalsSystem), typeof(SpawnPointSystem)]);
        SubscribeLocalEvent<RoundEndMessageEvent>(OnRoundEndMessage);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
        SubscribeLocalEvent<DropshipHijackStartEvent>(OnDropshipHijackStart);
        SubscribeLocalEvent<DropshipHijackLandedEvent>(OnDropshipHijackLanded);

        SubscribeLocalEvent<MarineComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<MarineComponent, ComponentRemove>(OnCompRemove);

        SubscribeLocalEvent<XenoComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<XenoComponent, ComponentRemove>(OnCompRemove);

        SubscribeLocalEvent<XenoEvolutionGranterComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<XenoComponent, ComponentInit>(OnXenoComponentInit);
        SubscribeLocalEvent<HiveMemberComponent, HiveChangedEvent>(OnHiveChanged);

        Subs.CVar(_config, RMCCVars.CMMarinesPerXeno, v => _marinesPerXeno = v, true);
        Subs.CVar(_config, RMCCVars.RMCAutoBalance, v => _autoBalance = v, true);
        Subs.CVar(_config, RMCCVars.RMCAutoBalanceStep, v => _autoBalanceStep = v, true);
        Subs.CVar(_config, RMCCVars.RMCAutoBalanceMax, v => _autoBalanceMax = v, true);
        Subs.CVar(_config, RMCCVars.RMCAutoBalanceMin, v => _autoBalanceMin = v, true);
        Subs.CVar(_config, RMCCVars.RMCMarinesPerSurvivor, v => _marinesPerSurvivor = v, true);
        Subs.CVar(_config, RMCCVars.RMCSurvivorsMaximum, v => _maximumSurvivors = v, true);
        Subs.CVar(_config, RMCCVars.RMCSurvivorsMinimum, v => _minimumSurvivors = v, true);
        Subs.CVar(_config, RMCCVars.RMCPlanetMapVoteExcludeLast, v => _mapVoteExcludeLast = v, true);
        Subs.CVar(_config, RMCCVars.RMCUseCarryoverVoting, v => _useCarryoverVoting = v, true);
        Subs.CVar(_config, RMCCVars.RMCLandingZoneMiasmaEnabled, v => _landingZoneMiasmaEnabled = v, true);
        Subs.CVar(_config, RMCCVars.RMCSunsetDuration, v => _sunsetDuration = TimeSpan.FromSeconds(v), true);
        Subs.CVar(_config, RMCCVars.RMCSunriseDuration, v => _sunriseDuration = TimeSpan.FromSeconds(v), true);
        Subs.CVar(_config, RMCCVars.RMCForceEndHijackTimeMinutes, v => _forceEndHijackTime = TimeSpan.FromMinutes(v), true);
        Subs.CVar(_config, RMCCVars.RMCHijackShipWeight, v => _hijackShipWeight = v, true);
        Subs.CVar(_config, RMCCVars.RMCMinimumHijackBurrowed, v => _hijackMinBurrowed = v, true);
        Subs.CVar(_config, RMCCVars.RMCDistressXenosMinimum, v => _xenosMinimum = v, true);
        Subs.CVar(_config, RMCCVars.RMCQueenBuildingBoost, v => _queenBuildingBoostEnabled = v, true);
        Subs.CVar(_config, RMCCVars.RMCQueenBuildingBoostDurationMinutes, v => _queenBoostDuration = TimeSpan.FromMinutes(v), true);
        Subs.CVar(_config, RMCCVars.RMCQueenBuildingBoostSpeedMultiplier, v => _queenBoostSpeedMultiplier = v, true);
        Subs.CVar(_config, RMCCVars.RMCQueenBuildingBoostRemoteRange, v => _queenBoostRemoteRange = v, true);

        ReloadPrototypes();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs ev)
    {
        if (ev.WasModified<EntityPrototype>())
            ReloadPrototypes();
    }

    private void OnMapLoading(LoadingMapsEvent ev)
    {
        SelectRandomPlanet();
        GameTicker.UpdateInfoText();
    }

    /// <summary>
    /// Main game loop tick for the distress signal rule.
    /// Handles faction scaling, queen boost removal, ARES announcements, and round end checks.
    /// </summary>
    protected override void ActiveTick(EntityUid uid, CMDistressSignalRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        var time = Timing.CurTime;
        component.StartTime ??= time;

        _scaling.TryStartScaling(component.MarineFaction);

        var announcementTime = time - component.StartTime;

        if (_queenBuildingBoostEnabled &&
            time - component.StartTime >= _queenBoostDuration &&
            !component.QueenBoostRemoved)
        {
            component.QueenBoostRemoved = true;
            RemoveQueenBuildingBoosts();
        }

        if (!component.RecalculatedPower)
        {
            component.RecalculatedPower = true;
            _rmcPower.RecalculatePower();
        }

        if (!component.AresGreetingDone && announcementTime >= component.AresGreetingDelay)
        {
            component.AresGreetingDone = true;

            if (component.StartARESAnnouncements)
                _marineAnnounce.AnnounceARESStaging(default, Loc.GetString("rmc-distress-signal-ares-online"), component.AresGreetingAudio,"rmc-announcement-ares-online");
        }

        if (!component.AresMapDone && announcementTime >= component.AresMapDelay)
        {
            component.AresMapDone = true;

            if (SelectedPlanetMap != null &&
                component.StartARESAnnouncements &&
                SelectedPlanetMap.Value.Comp.Announcement is { } announcement)
            {
                _marineAnnounce.AnnounceARESStaging(default, announcement, announcement: "rmc-announcement-ares-map");
            }
        }

        if (time >= component.NextCheck)
        {
            component.NextCheck = time + component.CheckEvery;
            CheckRoundShouldEnd();
        }

        if (component.EndAtAllClear != null &&
            time >= component.EndAtAllClear)
        {
            _roundEnd.EndRound();
            return;
        }

        if (_xenoEvolution.HasLiving<XenoEvolutionGranterComponent>(1))
            component.QueenDiedCheck = null;

        if (component.QueenDiedCheck == null)
            return;

        if (time >= component.QueenDiedCheck)
        {
            if (component.Hijack)
                EndRound(component, DistressSignalRuleResult.MinorXenoVictory);
            else if (_xenoEvolution.HasLiving<XenoComponent>(4))
                EndRound(component, DistressSignalRuleResult.MinorMarineVictory);
            else
                EndRound(component, DistressSignalRuleResult.MajorMarineVictory, "rmc-distress-signal-majormarinevictory-timeout");
        }
    }

    /// <summary>
    /// Sets a custom operation name for the current round, overriding the randomly generated one.
    /// </summary>
    /// <param name="customname">The custom operation name to use.</param>
    public void SetCustomOperationName(string customname)
    {
        OperationName = customname;
        _usingCustomOperationName = true;
    }

    private void EndRoundForQueenDeath(CMDistressSignalRuleComponent component)
    {
        if (component.Hijack)
            EndRound(component, DistressSignalRuleResult.MinorXenoVictory);
        else if (_xenoEvolution.HasLiving<XenoComponent>(QueenDeathXenoThreshold))
            EndRound(component, DistressSignalRuleResult.MinorMarineVictory);
        else
            EndRound(component, DistressSignalRuleResult.MajorMarineVictory, "rmc-distress-signal-majormarinevictory-timeout");
    }
}
