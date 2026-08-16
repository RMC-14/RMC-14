using System.Linq;
using System.Numerics;
using System.Text;
using Content.Server._RMC14.Dropship;
using Content.Server._RMC14.Marines;
using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.Ghost;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Humanoid.Components;
using Content.Server.Humanoid.Systems;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Server.Station.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared._RMC14.AlertLevel;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Dialog;
using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.ERT;
using Content.Shared._RMC14.Evacuation;
using Content.Shared._RMC14.Marines.Announce;
using Content.Shared._RMC14.Rules;
using Content.Shared.Buckle;
using Content.Shared.Database;
using Content.Shared.Doors.Components;
using Content.Shared.Ghost.Roles.Components;
using Content.Shared.GameTicking;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Prototypes;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Systems;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.ERT;

/// <summary>
/// Orchestrates the full ERT flow: request creation, admin approval, roster planning, ghost-role recruitment, shuttle launch and cleanup.
/// </summary>
public sealed partial class RMCERTSystem : EntitySystem
{
    [Dependency] private readonly AccessReaderSystem _access = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly RMCAlertLevelSystem _alertLevel = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly DialogSystem _dialog = default!;
    [Dependency] private readonly DropshipSystem _dropship = default!;
    [Dependency] private readonly SharedEvacuationSystem _evacuation = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly GhostSystem _ghost = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly MarineAnnounceSystem _marineAnnounce = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RandomHumanoidSystem _randomHumanoid = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    private readonly Dictionary<Guid, RMCERTRequest> _requests = new();
    private readonly Dictionary<EntityUid, TimeSpan> _sourceCooldowns = new();
    private readonly HashSet<ICommonSession> _queuedPendingAdminNotifications = [];
    private readonly HashSet<EntityUid> _shuttleSpawnObstacles = [];

    private MapId? _ertMap;
    private int _loadedShuttles;

    private static readonly SoundPathSpecifier DistressBeaconSound = new("/Audio/_RMC14/AI/distressbeacon.ogg");
    private static readonly SoundPathSpecifier DistressReceivedSound = new("/Audio/_RMC14/AI/distressreceived.ogg");
    private static readonly SoundPathSpecifier AdminRequestSound = new("/Audio/_RMC14/Effects/sos-morse-code.ogg");
    private static readonly EntProtoId ERTShuttleReturnDestinationPrototype = "RMCERTShuttleReturnDestination";
    private static readonly EntProtoId ERTShuttleReturnDestinationSmallPrototype = "RMCERTShuttleReturnDestinationSmall";
    private static readonly EntProtoId ERTShuttleReturnDestinationBigPrototype = "RMCERTShuttleReturnDestinationBig";
    // AdminManager raises OnPermsChanged before the client enables admin chat filters.
    private static readonly TimeSpan PendingAdminNotificationDelay = TimeSpan.FromSeconds(1);

    /// <inheritdoc />
    public override void Initialize()
    {
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        SubscribeLocalEvent<DropshipArrivedAtDestinationEvent>(OnDropshipArrivedAtDestination);

        SubscribeLocalEvent<RMCERTDistressBeaconComponent, UseInHandEvent>(OnHandheldUse);
        SubscribeLocalEvent<ActorComponent, RMCERTHandheldDistressReasonEvent>(OnHandheldReason);
        SubscribeLocalEvent<RMCERTMemberComponent, MindAddedMessage>(OnERTMindAdded);
        SubscribeLocalEvent<RMCERTShuttleComponent, FTLRequestEvent>(OnERTShuttleFTLRequested);
        SubscribeLocalEvent<RMCERTShuttleComponent, FTLStartedEvent>(OnERTShuttleFTLStarted);
        SubscribeLocalEvent<MarineCommunicationsComputerComponent, RMCERTConsoleDistressReasonEvent>(OnConsoleReason);
        Subs.BuiEvents<MarineCommunicationsComputerComponent>(MarineCommunicationsComputerUI.Key, subs =>
        {
            subs.Event<MarineCommunicationsDistressBeaconMsg>(OnMarineCommunicationsDistressBeacon);
        });

        _adminManager.OnPermsChanged += OnAdminPermsChanged;
        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
        ValidatePrototypes();
    }

    /// <inheritdoc />
    public override void Shutdown()
    {
        base.Shutdown();

        _adminManager.OnPermsChanged -= OnAdminPermsChanged;
        _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
    }

    /// <inheritdoc />
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var request in _requests.Values.ToArray())
        {
            TryAutoResolvePendingRequest(request);

            if (request.State != RMCERTRequestState.Recruiting)
                continue;

            if (request.SelectedCall is not { } callId || !_prototypes.TryIndex(callId, out var call))
                continue;

            if (!call.AutoLaunch)
                continue;

            if (_timing.CurTime < request.NextAutoLaunchAttempt)
                continue;

            TryAutoLaunch(request.Id);
        }
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _requests.Clear();
        _sourceCooldowns.Clear();
        _queuedPendingAdminNotifications.Clear();
        _ertMap = null;
        _loadedShuttles = 0;
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs ev)
    {
        if (ev.WasModified<RMCERTCallPrototype>() || ev.WasModified<EntityPrototype>())
            ValidatePrototypes();
    }
}
