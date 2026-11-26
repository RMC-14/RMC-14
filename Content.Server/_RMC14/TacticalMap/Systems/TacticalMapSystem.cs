using Content.Server.Administration.Logs;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.TacticalMap;
using Content.Shared.Actions;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Roles.Jobs;
using Content.Shared.GameTicking;
using Content.Shared.Mobs.Systems;
using Robust.Server.GameObjects;
using Robust.Server.GameStates;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.TacticalMap;

public sealed partial class TacticalMapSystem : SharedTacticalMapSystem
{
    private static readonly EntProtoId LiveFeedPrototype = "RMCTacticalMapLiveFeed";

    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly AreaSystem _area = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedJobSystem _jobs = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly PvsOverrideSystem _pvsOverride = default!;
    [Dependency] private readonly TacticalMapReplaySystem _replay = default!;
    [Dependency] private readonly SquadSystem _squad = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    private TimeSpan _announceCooldown;
    private TimeSpan _updateEvery;
    private TimeSpan _forceUpdateEvery;
    private TimeSpan _nextUpdate;
    private TimeSpan _nextForceUpdate;

    public override void Initialize()
    {
        base.Initialize();

        InitializeChannels();
        InitializeTracking();
        InitializeViewers();
        InitializeGrant();
        InitializeBaseMap();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);

        Subs.CVar(_config,
            RMCCVars.RMCTacticalMapAnnounceCooldownSeconds,
            v => _announceCooldown = TimeSpan.FromSeconds(v),
            true);

        Subs.CVar(_config,
            RMCCVars.RMCTacticalMapUpdateEverySeconds,
            v => _updateEvery = TimeSpan.FromSeconds(v),
            true);

        Subs.CVar(_config,
            RMCCVars.RMCTacticalMapForceUpdateEverySeconds,
            v => _forceUpdateEvery = TimeSpan.FromSeconds(v),
            true);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        ShutdownViewers();
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _dirtyTracked.Clear();
        _selfTracked.Clear();
        _viewerSessions.Clear();
        _actorSessions.Clear();
        _anySessionDirty = false;
        _nextForceUpdate = TimeSpan.Zero;
    }

    public override void Update(float frameTime)
    {
        UpdateBaseMaps();

        var time = _timing.CurTime;
        // full redo every so often in case an event was missed
        if (time >= _nextForceUpdate)
        {
            _nextForceUpdate = time + _forceUpdateEvery;
            ForceRefresh();
        }

        // blip moves are batched to this interval
        if (time >= _nextUpdate)
        {
            _nextUpdate = time + _updateEvery;
            FlushTracked();
        }

        RefreshDirtySessions();
    }

    private void ForceRefresh()
    {
        CleanupOrphanChannels();

        var query = EntityQueryEnumerator<ActiveTacticalMapTrackedComponent>();
        while (query.MoveNext(out var uid, out var active))
        {
            active.AppearanceDirty = true;
            active.MembershipDirty = true;
            _dirtyTracked.Add(uid);
        }

        MarkAllSessionsDirty();
    }
}
