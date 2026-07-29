using System.Linq;
using System.Numerics;
using Content.Server._RMC14.Rules.DistressSignal;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Body.Components;
using Content.Server.GameTicking.Events;
using Content.Server.Roles.Jobs;
using Content.Server.Warps;
using Content.Shared._RMC14.Ghost;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.Survivor;
using Content.Shared._RMC14.TacticalMap;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Follower;
using Content.Shared.Follower.Components;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.Ghost;
using Content.Shared.Humanoid;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Content.Shared.Roles;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Warps;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Ghost;

public sealed class RMCGhostTargetSystem : EntitySystem
{
    private static readonly ProtoId<NpcFactionPrototype> MarineFaction = "UNMC";
    private static readonly ProtoId<NpcFactionPrototype> XenoFaction = "RMCXeno";

    private static readonly LocId EmptyTitle = string.Empty;
    private static readonly LocId MarinesTitle = "rmc-ghost-target-window-group-marines";
    private static readonly LocId XenosTitle = "rmc-ghost-target-window-group-xenos";
    private static readonly LocId InfectedTitle = "rmc-ghost-target-window-group-infected";
    private static readonly LocId SurvivorsTitle = "rmc-ghost-target-window-group-survivors";
    private static readonly LocId EscapedTitle = "rmc-ghost-target-window-group-escaped";
    private static readonly LocId OthersTitle = "rmc-ghost-target-window-group-others";
    private static readonly LocId DeadsTitle = "rmc-ghost-target-window-group-deads";
    private static readonly LocId GhostsTitle = "rmc-ghost-target-window-group-ghosts";
    private static readonly LocId WarpPointsTitle = "rmc-ghost-target-window-group-warp-points";

    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly FollowerSystem _follower = default!;
    [Dependency] private readonly JobSystem _jobs = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SquadSystem _squad = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    private EntityQuery<GhostComponent> _ghostQuery;
    private EntityQuery<PhysicsComponent> _physicsQuery;
    private EntityUid? _store;
    private bool _initializingStore;

    public override void Initialize()
    {
        base.Initialize();

        _ghostQuery = GetEntityQuery<GhostComponent>();
        _physicsQuery = GetEntityQuery<PhysicsComponent>();

        SubscribeNetworkEvent<RMCGhostWarpsRequestEvent>(OnGhostWarpsRequest);
        SubscribeNetworkEvent<RMCGhostWarpToTargetRequestEvent>(OnGhostWarpToTargetRequest);
        SubscribeNetworkEvent<RMCGhostnadoRequestEvent>(OnGhostnadoRequest);

        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarting);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        SubscribeLocalEvent<DistressSignalEndgameChangedEvent>(OnDistressEndgameChanged);

        SubscribeLocalEvent<MindContainerComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<MindContainerComponent, MindRemovedMessage>(OnMindRemoved);
        SubscribeLocalEvent<RMCGhostTargetTrackedComponent, RoleAddedEvent>(OnTrackedRoleChanged);
        SubscribeLocalEvent<RMCGhostTargetTrackedComponent, RoleRemovedEvent>(OnTrackedRoleChanged);
        SubscribeLocalEvent<RMCGhostTargetTrackedComponent, MobStateChangedEvent>(OnTrackedStateChanged);
        SubscribeLocalEvent<RMCGhostTargetTrackedComponent, EntParentChangedMessage>(OnTrackedParentChanged);
        SubscribeLocalEvent<RMCGhostTargetTrackedComponent, DamageChangedEvent>(
            OnTrackedDamageChanged,
            after: [typeof(MobThresholdSystem)]);
        SubscribeLocalEvent<RMCGhostTargetTrackedComponent, EntityTerminatingEvent>(OnTrackedTerminating);

        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<PlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<EntityRenamedEvent>(OnEntityRenamed);
        SubscribeLocalEvent<EntityStartedFollowingEvent>(OnFollowerChanged);
        SubscribeLocalEvent<EntityStoppedFollowingEvent>(OnFollowerChanged);

        SubscribeLocalEvent<GhostComponent, GhostCanInteractChangedEvent>(OnGhostVisibilityChanged);
        SubscribeLocalEvent<GhostComponent, EntityTerminatingEvent>(OnGhostTerminating);

        SubscribeLocalEvent<WarpPointComponent, MapInitEvent>(OnWarpPointMapInit);
        SubscribeLocalEvent<WarpPointComponent, WarpPointLocationChangedEvent>(OnWarpPointLocationChanged);
        SubscribeLocalEvent<WarpPointComponent, EntityTerminatingEvent>(OnWarpPointTerminating);

        SubscribeLocalEvent<NpcFactionMemberComponent, NpcFactionMembershipChangedEvent>(OnFactionChanged);
        SubscribeLocalEvent<VisitingMindComponent, MindVisitedMessage>(OnMindVisited);
        SubscribeLocalEvent<SquadMemberAddedEvent>(OnSquadMemberAdded);
        SubscribeLocalEvent<SquadMemberRemovedEvent>(OnSquadMemberRemoved);
        SubscribeLocalEvent<RMCGhostTargetTrackedComponent, SquadMemberUpdatedEvent>(OnSquadMemberUpdated);

        SubscribeLocalEvent<TacticalMapIconComponent, TacticalMapIconChangedEvent>(OnTacticalIconChanged);

        EntityManager.ComponentAdded += OnComponentAdded;
        EntityManager.ComponentRemoved += OnComponentRemoved;
    }

    public override void Shutdown()
    {
        EntityManager.ComponentAdded -= OnComponentAdded;
        EntityManager.ComponentRemoved -= OnComponentRemoved;
        base.Shutdown();
    }

    private void OnRoundStarting(RoundStartingEvent args)
    {
        if (TryGetStore(out var store))
            InitializeStore(store);
        else
            EnsureStore();
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent args)
    {
        if (_store is { } store && Exists(store))
            QueueDel(store);

        _store = null;

        var tracked = EntityQueryEnumerator<RMCGhostTargetTrackedComponent>();
        while (tracked.MoveNext(out var uid, out _))
            RemCompDeferred<RMCGhostTargetTrackedComponent>(uid);
    }

    private void OnGhostWarpsRequest(RMCGhostWarpsRequestEvent msg, EntitySessionEventArgs args)
    {
        if (!TryGetSenderGhost(args, out var ghost))
        {
            Log.Warning($"User {args.SenderSession.Name} sent a {nameof(RMCGhostWarpsRequestEvent)} without being a ghost.");
            return;
        }

        var store = EnsureStore();
        var view = GetView(store.Comp, _adminManager.IsAdmin(args.SenderSession));
        var response = new RMCGhostWarpsResponseEvent(
            msg.RequestId,
            store.Comp.Revision,
            GetNetEntity(ghost),
            view.Targets,
            view.Sections);
        RaiseNetworkEvent(response, args.SenderSession.Channel);
    }

    private void OnGhostWarpToTargetRequest(RMCGhostWarpToTargetRequestEvent msg, EntitySessionEventArgs args)
    {
        if (!TryGetSenderGhost(args, out var ghost))
        {
            Log.Warning($"User {args.SenderSession.Name} tried to RMC ghost warp without being a ghost.");
            return;
        }

        var target = GetEntity(msg.Target);
        var store = EnsureStore();
        var view = GetView(store.Comp, _adminManager.IsAdmin(args.SenderSession));
        if (!Exists(target) ||
            target == ghost ||
            !view.AllowedTargets.Contains(target))
        {
            Log.Warning($"User {args.SenderSession.Name} tried to RMC ghost warp to a target outside their prepared view: {msg.Target}");
            return;
        }

        WarpTo(ghost, target);
    }

    private void OnGhostnadoRequest(RMCGhostnadoRequestEvent msg, EntitySessionEventArgs args)
    {
        if (!TryGetSenderGhost(args, out var ghost))
        {
            Log.Warning($"User {args.SenderSession.Name} tried to RMC ghostnado without being a ghost.");
            return;
        }

        if (_follower.GetMostGhostFollowed() is not { } target)
            return;

        WarpTo(ghost, target);
    }

    private static RMCGhostTargetPreparedView GetView(RMCGhostTargetStoreComponent store, bool admin)
    {
        return admin ? store.Admin : store.Public;
    }

    private bool TryGetSenderGhost(EntitySessionEventArgs args, out EntityUid ghost)
    {
        ghost = default;
        if (args.SenderSession.AttachedEntity is not { Valid: true } attached ||
            !_ghostQuery.HasComp(attached))
        {
            return false;
        }

        ghost = attached;
        return true;
    }

    private Entity<RMCGhostTargetStoreComponent> EnsureStore()
    {
        if (_store is { } storeUid &&
            TryComp(storeUid, out RMCGhostTargetStoreComponent? existing))
        {
            return (storeUid, existing);
        }

        var query = EntityQueryEnumerator<RMCGhostTargetStoreComponent>();
        if (query.MoveNext(out storeUid, out existing))
        {
            _store = storeUid;
            if (!existing.IsInitialized)
                InitializeStore((storeUid, existing));

            while (query.MoveNext(out var duplicate, out _))
            {
                Log.Error($"Removing duplicate {nameof(RMCGhostTargetStoreComponent)} from {ToPrettyString(duplicate)}.");
                QueueDel(duplicate);
            }

            return (storeUid, existing);
        }

        storeUid = Spawn(null, MapCoordinates.Nullspace);
        existing = EnsureComp<RMCGhostTargetStoreComponent>(storeUid);
        _store = storeUid;
        InitializeStore((storeUid, existing));
        return (storeUid, existing);
    }

    internal Entity<RMCGhostTargetStoreComponent> EnsureStoreForTests()
    {
        return EnsureStore();
    }

    internal void RefreshTargetForTests(EntityUid uid)
    {
        RefreshTarget(uid);
    }

    internal (uint Revision, List<RMCGhostTargetEntry> Targets, List<RMCGhostTargetSection> Sections)
        GetPublicViewForTests()
    {
        var store = EnsureStore().Comp;
        return (store.Revision, store.Public.Targets, store.Public.Sections);
    }

    internal RMCGhostTargetEntry? GetTargetEntryForTests(EntityUid uid)
    {
        var store = EnsureStore().Comp;
        return store.Records.TryGetValue(uid, out var record)
            ? record.Entry
            : null;
    }

    internal bool IsTargetAllowedForTests(EntityUid uid, bool admin = false)
    {
        var store = EnsureStore().Comp;
        return GetView(store, admin).AllowedTargets.Contains(uid);
    }

    private void InitializeStore(Entity<RMCGhostTargetStoreComponent> store)
    {
        if (_initializingStore)
            return;

        _initializingStore = true;
        try
        {
            store.Comp.Records.Clear();
            store.Comp.MindTargets.Clear();
            store.Comp.Public.Clear();
            store.Comp.Admin.Clear();
            store.Comp.Revision = 0;
            store.Comp.DistressEndgame = IsDistressEndgame();

            BuildFactionDefinitions(store.Comp);
            InitializeSectionIndex(store.Comp);
            SeedStore(store.Comp);
            RebuildViews(store.Comp);
            store.Comp.IsInitialized = true;
        }
        finally
        {
            _initializingStore = false;
        }
    }

    private void SeedStore(RMCGhostTargetStoreComponent store)
    {
        var minds = EntityQueryEnumerator<MindContainerComponent>();
        while (minds.MoveNext(out var uid, out var mind))
        {
            if (mind.Mind != null)
                EnsureComp<RMCGhostTargetTrackedComponent>(uid);
        }

        var tracked = EntityQueryEnumerator<RMCGhostTargetTrackedComponent>();
        while (tracked.MoveNext(out var uid, out _))
            UpsertRecord(store, uid);

        foreach (var session in _player.Sessions)
        {
            if (session.AttachedEntity is { Valid: true } attached &&
                _ghostQuery.HasComp(attached))
            {
                UpsertRecord(store, attached);
            }
        }

        var warpPoints = EntityQueryEnumerator<WarpPointComponent>();
        while (warpPoints.MoveNext(out var uid, out _))
            UpsertRecord(store, uid);
    }

    private void OnMindAdded(Entity<MindContainerComponent> ent, ref MindAddedMessage args)
    {
        EnsureComp<RMCGhostTargetTrackedComponent>(ent);
        RefreshTarget(ent);
    }

    private void OnMindRemoved(Entity<MindContainerComponent> ent, ref MindRemovedMessage args)
    {
        RefreshTarget(ent);
    }

    private void OnTrackedRoleChanged<T>(Entity<RMCGhostTargetTrackedComponent> ent, ref T args)
        where T : RoleEvent
    {
        RefreshTarget(ent);
    }

    private void OnTrackedStateChanged(Entity<RMCGhostTargetTrackedComponent> ent, ref MobStateChangedEvent args)
    {
        RefreshTarget(ent);
    }

    private void OnTrackedParentChanged(Entity<RMCGhostTargetTrackedComponent> ent, ref EntParentChangedMessage args)
    {
        RefreshTarget(ent);
    }

    private void OnTrackedDamageChanged(Entity<RMCGhostTargetTrackedComponent> ent, ref DamageChangedEvent args)
    {
        RefreshEntry(ent);
    }

    private void OnTrackedTerminating(Entity<RMCGhostTargetTrackedComponent> ent, ref EntityTerminatingEvent args)
    {
        RemoveTarget(ent);
    }

    private void OnPlayerAttached(PlayerAttachedEvent args)
    {
        RefreshTarget(args.Entity);
    }

    private void OnPlayerDetached(PlayerDetachedEvent args)
    {
        if (!TryGetStore(out var store) ||
            !store.Comp.Records.TryGetValue(args.Entity, out var record))
        {
            return;
        }

        if (record.Kind == RMCGhostTargetRecordKind.Ghost)
            RemoveTarget(args.Entity);
        else
            RefreshTarget(args.Entity);
    }

    private void OnEntityRenamed(ref EntityRenamedEvent args)
    {
        if (!TryGetStore(out var store))
            return;

        if (store.Comp.Records.ContainsKey(args.Uid))
        {
            RefreshTarget(args.Uid);
            return;
        }

        if (HasComp<SquadTeamComponent>(args.Uid))
        {
            var key = new RMCGhostTargetSectionKey(
                RMCGhostTargetSectionKind.Squad,
                Entity: GetNetEntity(args.Uid));
            if (store.Comp.Sections.TryGetValue(key, out var section))
            {
                section.Title = Name(args.Uid);
                section.Parent?.Children.Sort(CompareSectionsByTitle);
            }

            RebuildViews(store.Comp);
        }
    }

    private void OnFollowerChanged(FollowEvent args)
    {
        RefreshEntry(args.Following);
    }

    private void OnGhostVisibilityChanged(
        EntityUid uid,
        GhostComponent component,
        GhostCanInteractChangedEvent args)
    {
        RefreshTarget(uid);
    }

    private void OnGhostTerminating(Entity<GhostComponent> ent, ref EntityTerminatingEvent args)
    {
        RemoveTarget(ent);
    }

    private void OnWarpPointMapInit(Entity<WarpPointComponent> ent, ref MapInitEvent args)
    {
        RefreshTarget(ent);
    }

    private void OnWarpPointLocationChanged(
        EntityUid uid,
        WarpPointComponent component,
        WarpPointLocationChangedEvent args)
    {
        RefreshTarget(uid);
    }

    private void OnWarpPointTerminating(Entity<WarpPointComponent> ent, ref EntityTerminatingEvent args)
    {
        RemoveTarget(ent);
    }

    private void OnFactionChanged(
        EntityUid uid,
        NpcFactionMemberComponent component,
        NpcFactionMembershipChangedEvent args)
    {
        RefreshTarget(uid);
    }

    private void OnMindVisited(EntityUid uid, VisitingMindComponent component, MindVisitedMessage args)
    {
        RefreshTarget(uid);
    }

    private void OnSquadMemberAdded(ref SquadMemberAddedEvent args)
    {
        RefreshTarget(args.Member);
    }

    private void OnSquadMemberRemoved(ref SquadMemberRemovedEvent args)
    {
        RefreshTarget(args.Member);
    }

    private void OnSquadMemberUpdated(
        Entity<RMCGhostTargetTrackedComponent> ent,
        ref SquadMemberUpdatedEvent args)
    {
        RefreshTarget(ent);
    }

    private void OnComponentAdded(AddedComponentEventArgs args)
    {
        OnRelevantComponentChanged(args.BaseArgs);
    }

    private void OnComponentRemoved(RemovedComponentEventArgs args)
    {
        OnRelevantComponentChanged(args.BaseArgs);
    }

    private void OnRelevantComponentChanged(ComponentEventArgs args)
    {
        switch (args.Component)
        {
            case GhostComponent:
            case MindContainerComponent:
            case VisitingMindComponent:
            case VictimInfectedComponent:
            case RMCSurvivorComponent:
            case XenoComponent:
            case HumanoidAppearanceComponent:
            case NpcFactionMemberComponent:
                RefreshTarget(args.Owner);
                break;
            case WarpPointComponent:
                if (LifeStage(args.Owner) >= EntityLifeStage.MapInitialized)
                    RefreshTarget(args.Owner);
                break;
            case TacticalMapIconComponent:
            case DamageableComponent:
            case MobThresholdsComponent:
                RefreshEntry(args.Owner);
                break;
            case AlmayerComponent:
                OnAlmayerChanged();
                break;
        }
    }

    private void OnTacticalIconChanged(
        EntityUid uid,
        TacticalMapIconComponent component,
        TacticalMapIconChangedEvent args)
    {
        RefreshEntry(uid);
    }

    private void OnAlmayerChanged()
    {
        if (TryGetStore(out var store) && store.Comp.DistressEndgame)
        {
            RebuildAllMemberships(store.Comp);
            RebuildViews(store.Comp);
        }
    }

    private void OnDistressEndgameChanged(DistressSignalEndgameChangedEvent args)
    {
        if (!TryGetStore(out var store) ||
            store.Comp.DistressEndgame == args.Active)
        {
            return;
        }

        store.Comp.DistressEndgame = args.Active;
        RebuildAllMemberships(store.Comp);
        RebuildViews(store.Comp);
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (!args.WasModified<NpcFactionPrototype>() ||
            !TryGetStore(out var store))
        {
            return;
        }

        BuildFactionDefinitions(store.Comp);
        InitializeSectionIndex(store.Comp);
        RebuildAllMemberships(store.Comp, true);
        RebuildViews(store.Comp);
    }

    private bool TryGetStore(out Entity<RMCGhostTargetStoreComponent> store)
    {
        if (_initializingStore ||
            _store is not { } uid ||
            !TryComp(uid, out RMCGhostTargetStoreComponent? component))
        {
            store = default;
            return false;
        }

        store = (uid, component);
        return true;
    }

    private void RefreshTarget(EntityUid uid)
    {
        if (_initializingStore ||
            !TryGetStore(out var store))
        {
            return;
        }

        UpsertRecord(store.Comp, uid);
        RebuildViews(store.Comp);
    }

    private void RefreshEntry(EntityUid uid)
    {
        if (_initializingStore ||
            !TryGetStore(out var store) ||
            !store.Comp.Records.TryGetValue(uid, out var record) ||
            !TryBuildEntry(uid, record.Kind, out var entry))
        {
            return;
        }

        record.Entry = entry;
        UpdatePreparedEntry(store.Comp.Public, uid, entry);
        UpdatePreparedEntry(store.Comp.Admin, uid, entry);
        store.Comp.Revision++;
    }

    private static void UpdatePreparedEntry(
        RMCGhostTargetPreparedView view,
        EntityUid uid,
        RMCGhostTargetEntry entry)
    {
        if (view.TargetIndices.TryGetValue(uid, out var index))
            view.Targets[index] = entry;
    }

    private void RemoveTarget(EntityUid uid)
    {
        if (_initializingStore ||
            !TryGetStore(out var store) ||
            !RemoveRecord(store.Comp, uid))
        {
            return;
        }

        RebuildViews(store.Comp);
    }

    private void UpsertRecord(RMCGhostTargetStoreComponent store, EntityUid uid)
    {
        if (store.Records.TryGetValue(uid, out var old))
        {
            RemoveMemberships(store, old);
            if (old.Mind is { } oldMind)
                RemoveMindTarget(store, oldMind, uid);
        }

        if (!TryBuildRecord(uid, out var record))
        {
            store.Records.Remove(uid);
            return;
        }

        BuildMemberships(store, record);
        store.Records[uid] = record;
        if (record.Mind is { } mind)
            AddMindTarget(store, mind, uid);

        AddMemberships(store, record);
    }

    private bool RemoveRecord(RMCGhostTargetStoreComponent store, EntityUid uid)
    {
        if (!store.Records.Remove(uid, out var record))
            return false;

        RemoveMemberships(store, record);
        if (record.Mind is { } mind)
            RemoveMindTarget(store, mind, uid);

        return true;
    }

    private static void AddMindTarget(
        RMCGhostTargetStoreComponent store,
        EntityUid mind,
        EntityUid target)
    {
        if (!store.MindTargets.TryGetValue(mind, out var targets))
        {
            targets = new HashSet<EntityUid>();
            store.MindTargets.Add(mind, targets);
        }

        targets.Add(target);
    }

    private static void RemoveMindTarget(
        RMCGhostTargetStoreComponent store,
        EntityUid mind,
        EntityUid target)
    {
        if (!store.MindTargets.TryGetValue(mind, out var targets))
            return;

        targets.Remove(target);
        if (targets.Count == 0)
            store.MindTargets.Remove(mind);
    }

    private bool TryBuildRecord(EntityUid uid, out RMCGhostTargetRecord record)
    {
        record = default!;
        if (TerminatingOrDeleted(uid))
            return false;

        RMCGhostTargetRecordKind kind;
        if (HasComp<WarpPointComponent>(uid))
        {
            kind = RMCGhostTargetRecordKind.WarpPoint;
        }
        else if (_ghostQuery.HasComp(uid))
        {
            if (!_player.TryGetSessionByEntity(uid, out _))
                return false;

            kind = RMCGhostTargetRecordKind.Ghost;
        }
        else
        {
            if (!HasComp<RMCGhostTargetTrackedComponent>(uid) ||
                !HasComp<MindContainerComponent>(uid) ||
                HasComp<BrainComponent>(uid) ||
                HasComp<BorgBrainComponent>(uid) ||
                HasComp<MMIComponent>(uid))
            {
                return false;
            }

            kind = RMCGhostTargetRecordKind.Body;
        }

        if (!TryBuildEntry(uid, kind, out var entry))
            return false;

        var mind = kind == RMCGhostTargetRecordKind.WarpPoint
            ? null
            : GetMindId(uid);
        var adminGhost = kind == RMCGhostTargetRecordKind.Ghost &&
                         Comp<GhostComponent>(uid).CanGhostInteract;
        record = new RMCGhostTargetRecord(uid, kind, entry, mind, adminGhost);
        return true;
    }

    private bool TryBuildEntry(
        EntityUid uid,
        RMCGhostTargetRecordKind kind,
        out RMCGhostTargetEntry entry)
    {
        entry = default;
        if (TerminatingOrDeleted(uid))
            return false;

        if (kind == RMCGhostTargetRecordKind.WarpPoint)
        {
            if (!TryComp(uid, out WarpPointComponent? warp))
                return false;

            entry = new RMCGhostTargetEntry(
                GetNetEntity(uid),
                warp.Location ?? Name(uid),
                null,
                RMCGhostTargetFlags.WarpPoint,
                GetFollowerCount(uid),
                RMCGhostTargetHealthState.None,
                0,
                null,
                null,
                RMCGhostTargetTooltipJobKind.None);
            return true;
        }

        var displayName = Name(uid);
        var hasJob = TryGetJobName(uid, out var jobName);
        var tooltipKind = !hasJob
            ? RMCGhostTargetTooltipJobKind.None
            : HasComp<XenoComponent>(uid)
                ? RMCGhostTargetTooltipJobKind.Caste
                : RMCGhostTargetTooltipJobKind.Job;

        var health = kind == RMCGhostTargetRecordKind.Body
            ? GetHealthStatus(uid)
            : (RMCGhostTargetHealthState.None, (byte) 0);
        var tactical = kind == RMCGhostTargetRecordKind.Body
            ? GetTacticalIcons(uid)
            : (null, null);

        entry = new RMCGhostTargetEntry(
            GetNetEntity(uid),
            displayName,
            jobName,
            RMCGhostTargetFlags.None,
            GetFollowerCount(uid),
            health.Item1,
            health.Item2,
            tactical.Item1,
            tactical.Item2,
            tooltipKind);
        return true;
    }

    private bool TryGetJobName(EntityUid uid, out string? jobName)
    {
        if (_jobs.MindTryGetJobName(GetMindId(uid), out var name))
        {
            jobName = name;
            return true;
        }

        jobName = null;
        return false;
    }

    private EntityUid? GetMindId(EntityUid uid)
    {
        if (TryComp(uid, out MindContainerComponent? mindContainer) &&
            mindContainer.Mind is { } mind)
        {
            return mind;
        }

        return TryComp(uid, out VisitingMindComponent? visiting)
            ? visiting.MindId
            : null;
    }

    private (RMCGhostTargetHealthState State, byte Percent) GetHealthStatus(EntityUid uid)
    {
        if (!_mobState.IsCritical(uid) && !_mobState.IsAlive(uid))
            return (RMCGhostTargetHealthState.None, 0);

        if (!TryComp(uid, out DamageableComponent? damageable) ||
            !TryComp(uid, out MobThresholdsComponent? thresholds) ||
            !_mobThreshold.TryGetThresholdForState(uid, MobState.Dead, out var deadThreshold, thresholds))
        {
            return (RMCGhostTargetHealthState.None, 0);
        }

        var maxHealth = deadThreshold.Value.Float();
        if (maxHealth <= 0)
            return (RMCGhostTargetHealthState.None, 0);

        var currentHealth = maxHealth - damageable.TotalDamage.Float();
        var percent = (byte) Math.Clamp((int) MathF.Round(currentHealth / maxHealth * 100f), 0, 100);
        var state = percent >= 80
            ? RMCGhostTargetHealthState.High
            : percent >= 40
                ? RMCGhostTargetHealthState.Medium
                : RMCGhostTargetHealthState.Low;
        return (state, percent);
    }

    private (SpriteSpecifier.Rsi? Icon, SpriteSpecifier.Rsi? Background) GetTacticalIcons(EntityUid uid)
    {
        return TryComp(uid, out TacticalMapIconComponent? icon)
            ? (icon.Icon, icon.Background)
            : (null, null);
    }

    private int GetFollowerCount(EntityUid uid)
    {
        return TryComp(uid, out FollowedComponent? followed)
            ? followed.Following.Count
            : 0;
    }

    private void RebuildViews(RMCGhostTargetStoreComponent store)
    {
        BuildView(store, store.Public, false);
        BuildView(store, store.Admin, true);
        store.Revision++;
    }

    private void BuildView(
        RMCGhostTargetStoreComponent store,
        RMCGhostTargetPreparedView view,
        bool showAdminGhosts)
    {
        view.Clear();
        foreach (var record in store.Records.Values)
        {
            if (!showAdminGhosts &&
                record.Kind == RMCGhostTargetRecordKind.Ghost &&
                record.AdminGhost)
            {
                continue;
            }

            view.TargetIndices[record.Uid] = view.Targets.Count;
            view.Targets.Add(record.Entry);
            view.AllowedTargets.Add(record.Uid);
        }

        view.Sections = BuildSections(store, view.AllowedTargets);
    }

    private List<RMCGhostTargetSection> BuildSections(
        RMCGhostTargetStoreComponent store,
        HashSet<EntityUid> allowedTargets)
    {
        var result = new List<RMCGhostTargetSection>();
        foreach (var root in store.SectionRoots)
        {
            if (ToSection(root, allowedTargets) is { } section)
                result.Add(section);
        }

        return result;
    }

    private RMCGhostTargetSection? ToSection(
        RMCGhostTargetStoredSection section,
        HashSet<EntityUid> allowedTargets)
    {
        var targets = new List<NetEntity>(section.Entries.Count);
        foreach (var entry in section.Entries)
        {
            if (allowedTargets.Contains(entry.Uid))
                targets.Add(GetNetEntity(entry.Uid));
        }

        var children = new List<RMCGhostTargetSection>(section.Children.Count);
        foreach (var child in section.Children)
        {
            if (ToSection(child, allowedTargets) is { } childSection)
                children.Add(childSection);
        }

        if (targets.Count == 0 && children.Count == 0)
            return null;

        return new RMCGhostTargetSection(
            section.Key,
            section.TitleLocId,
            section.Title,
            section.HeaderColor,
            section.IsExpandedByDefault,
            targets,
            children);
    }

    private void BuildMemberships(
        RMCGhostTargetStoreComponent store,
        RMCGhostTargetRecord target)
    {
        var uid = target.Uid;
        if (target.Kind == RMCGhostTargetRecordKind.WarpPoint)
        {
            AddMembership(target, RMCGhostTargetSectionKind.WarpPoints);
            return;
        }

        if (target.Kind == RMCGhostTargetRecordKind.Ghost)
        {
            AddMembership(target, RMCGhostTargetSectionKind.Ghosts);
            return;
        }

        var isInfected = HasComp<VictimInfectedComponent>(uid);
        var isSurvivor = HasComp<RMCSurvivorComponent>(uid);
        var isEscaped = IsEscaped(uid, store.DistressEndgame);

        if (_mobState.IsDead(uid))
        {
            AddMembership(target, RMCGhostTargetSectionKind.Dead);
            if (isInfected)
                AddMembership(target, RMCGhostTargetSectionKind.Infected);

            return;
        }

        if (isInfected)
            AddMembership(target, RMCGhostTargetSectionKind.Infected);

        if (isSurvivor)
        {
            AddMembership(target, RMCGhostTargetSectionKind.Survivors);
            if (isEscaped)
                AddMembership(target, RMCGhostTargetSectionKind.Escaped);

            return;
        }

        if (isEscaped)
        {
            AddMembership(target, RMCGhostTargetSectionKind.Escaped);
            return;
        }

        var factions = _npcFaction.GetFactionMembership(uid);
        var isMarine = false;
        foreach (var faction in factions)
        {
            if (faction == MarineFaction)
            {
                isMarine = true;
                continue;
            }

            if (faction == XenoFaction)
                continue;

            var key = new RMCGhostTargetSectionKey(
                RMCGhostTargetSectionKind.Faction,
                faction.ToString());
            if (!store.Sections.ContainsKey(key))
                continue;

            target.Memberships.Add(new RMCGhostTargetMembership(key));
            return;
        }

        if (isMarine)
        {
            AddMarineMembership(store, target);
            return;
        }

        if (TryComp(uid, out XenoComponent? xeno))
        {
            AddMembership(target, RMCGhostTargetSectionKind.Xenos, xeno.Tier);
            return;
        }

        AddMembership(target, RMCGhostTargetSectionKind.Others);
    }

    private static void AddMembership(
        RMCGhostTargetRecord target,
        RMCGhostTargetSectionKind kind,
        int? sortValue = null)
    {
        target.Memberships.Add(new RMCGhostTargetMembership(
            new RMCGhostTargetSectionKey(kind),
            sortValue));
    }

    private void AddMarineMembership(
        RMCGhostTargetStoreComponent store,
        RMCGhostTargetRecord target)
    {
        var authorityLevel = GetMarineAuthorityLevel(target.Uid);
        if (!_squad.TryGetMemberSquad(target.Uid, out var squad))
        {
            AddMembership(target, RMCGhostTargetSectionKind.MarineOthers, authorityLevel);
            return;
        }

        var key = new RMCGhostTargetSectionKey(
            RMCGhostTargetSectionKind.Squad,
            Entity: GetNetEntity(squad));
        if (!store.Sections.TryGetValue(key, out var section))
        {
            _squad.TryGetSquadMemberColor(target.Uid, out var color);
            section = new RMCGhostTargetStoredSection(
                key,
                EmptyTitle,
                Name(squad),
                AdjustLightness(color, -0.1f),
                true,
                true);
            var marines = GetSection(store, RMCGhostTargetSectionKind.Marines);
            AddChildSection(marines, section);
            store.Sections.Add(key, section);
        }

        target.Memberships.Add(new RMCGhostTargetMembership(key, authorityLevel));
    }

    private void AddMemberships(
        RMCGhostTargetStoreComponent store,
        RMCGhostTargetRecord target)
    {
        foreach (var membership in target.Memberships)
        {
            if (!store.Sections.TryGetValue(membership.Section, out var section))
                continue;

            var entry = new RMCGhostTargetStoredEntry(target.Uid, membership.SortValue);
            var low = 0;
            var high = section.Entries.Count;
            while (low < high)
            {
                var middle = low + (high - low) / 2;
                if (CompareEntries(store, section.Entries[middle], entry) <= 0)
                    low = middle + 1;
                else
                    high = middle;
            }

            section.Entries.Insert(low, entry);
        }
    }

    private void RemoveMemberships(
        RMCGhostTargetStoreComponent store,
        RMCGhostTargetRecord target)
    {
        foreach (var membership in target.Memberships)
        {
            if (!store.Sections.TryGetValue(membership.Section, out var section))
                continue;

            var index = section.Entries.FindIndex(entry => entry.Uid == target.Uid);
            if (index >= 0)
                section.Entries.RemoveAt(index);

            if (!section.IsDynamic || section.Entries.Count != 0)
                continue;

            section.Parent?.Children.Remove(section);
            store.Sections.Remove(section.Key);
        }

        target.Memberships.Clear();
    }

    private void RebuildAllMemberships(
        RMCGhostTargetStoreComponent store,
        bool sectionIndexWasReset = false)
    {
        if (!sectionIndexWasReset)
        {
            foreach (var target in store.Records.Values)
                RemoveMemberships(store, target);
        }
        else
        {
            foreach (var target in store.Records.Values)
                target.Memberships.Clear();
        }

        foreach (var target in store.Records.Values)
        {
            BuildMemberships(store, target);
            AddMemberships(store, target);
        }
    }

    private static RMCGhostTargetStoredSection GetSection(
        RMCGhostTargetStoreComponent store,
        RMCGhostTargetSectionKind kind)
    {
        return store.Sections[new RMCGhostTargetSectionKey(kind)];
    }

    private void AddChildSection(
        RMCGhostTargetStoredSection parent,
        RMCGhostTargetStoredSection child)
    {
        child.Parent = parent;
        parent.Children.Add(child);
        parent.Children.Sort(CompareSectionsByTitle);
    }

    private int? GetMarineAuthorityLevel(EntityUid uid)
    {
        return _jobs.MindTryGetJob(GetMindId(uid), out var job)
            ? job.MarineAuthorityLevel
            : null;
    }

    private bool IsDistressEndgame()
    {
        var query = EntityQueryEnumerator<ActiveGameRuleComponent, CMDistressSignalRuleComponent>();
        while (query.MoveNext(out _, out var distress))
        {
            if (distress.Hijack || distress.ForceEndAt != null)
                return true;
        }

        return false;
    }

    private bool IsEscaped(EntityUid uid, bool distressEndgame)
    {
        if (!distressEndgame ||
            !HasComp<HumanoidAppearanceComponent>(uid) ||
            HasComp<XenoComponent>(uid))
        {
            return false;
        }

        return !HasComp<AlmayerComponent>(Transform(uid).MapUid);
    }

    private void BuildFactionDefinitions(RMCGhostTargetStoreComponent store)
    {
        store.FactionSections.Clear();
        store.FactionRoots.Clear();

        var prototypes = new Dictionary<ProtoId<NpcFactionPrototype>, NpcFactionPrototype>();
        foreach (var prototype in _prototypes.EnumeratePrototypes<NpcFactionPrototype>())
        {
            if (HasFactionSection(prototype))
                prototypes[prototype.ID] = prototype;
        }

        var childParents = new Dictionary<ProtoId<NpcFactionPrototype>, ProtoId<NpcFactionPrototype>>();
        foreach (var prototype in prototypes.Values)
        {
            BuildFactionDefinition(
                prototype.ID,
                prototypes,
                store.FactionSections,
                childParents,
                new Stack<ProtoId<NpcFactionPrototype>>());
        }

        foreach (var (id, section) in store.FactionSections)
        {
            if (!childParents.ContainsKey(id))
                store.FactionRoots.Add(section);
        }
    }

    private void InitializeSectionIndex(RMCGhostTargetStoreComponent store)
    {
        store.Sections.Clear();
        store.SectionRoots.Clear();

        AddRootSection(store, RMCGhostTargetSectionKind.Marines, MarinesTitle, Color.FromHex("#1c70b0"));
        AddRootSection(store, RMCGhostTargetSectionKind.Xenos, XenosTitle, Color.FromHex("#472f4f"));
        AddRootSection(store, RMCGhostTargetSectionKind.Infected, InfectedTitle, Color.FromHex("#8f4f24"));
        AddRootSection(store, RMCGhostTargetSectionKind.Survivors, SurvivorsTitle, Color.FromHex("#3f7f4f"));
        AddRootSection(
            store,
            RMCGhostTargetSectionKind.Escaped,
            EscapedTitle,
            Color.FromHex("#808000"),
            false);

        var factionRoots = store.FactionRoots
            .Select(definition => CreateFactionSection(store, definition))
            .ToList();
        factionRoots.Sort(CompareSectionsByTitle);
        store.SectionRoots.AddRange(factionRoots);

        AddRootSection(store, RMCGhostTargetSectionKind.Others, OthersTitle);
        AddRootSection(store, RMCGhostTargetSectionKind.Dead, DeadsTitle, isExpandedByDefault: false);
        AddRootSection(store, RMCGhostTargetSectionKind.WarpPoints, WarpPointsTitle, isExpandedByDefault: false);
        AddRootSection(store, RMCGhostTargetSectionKind.Ghosts, GhostsTitle, isExpandedByDefault: false);

        var marineOthers = new RMCGhostTargetStoredSection(
            new RMCGhostTargetSectionKey(RMCGhostTargetSectionKind.MarineOthers),
            OthersTitle,
            null,
            Color.FromHex("#3c3c3c"),
            true);
        store.Sections.Add(marineOthers.Key, marineOthers);
        AddChildSection(GetSection(store, RMCGhostTargetSectionKind.Marines), marineOthers);
    }

    private void AddRootSection(
        RMCGhostTargetStoreComponent store,
        RMCGhostTargetSectionKind kind,
        LocId title,
        Color? color = null,
        bool isExpandedByDefault = true)
    {
        var section = new RMCGhostTargetStoredSection(
            new RMCGhostTargetSectionKey(kind),
            title,
            null,
            color ?? Color.FromHex("#3c3c3c"),
            isExpandedByDefault);
        store.Sections.Add(section.Key, section);
        store.SectionRoots.Add(section);
    }

    private RMCGhostTargetStoredSection CreateFactionSection(
        RMCGhostTargetStoreComponent store,
        RMCGhostFactionSectionDefinition definition)
    {
        var key = new RMCGhostTargetSectionKey(
            RMCGhostTargetSectionKind.Faction,
            definition.Id.ToString());
        var section = new RMCGhostTargetStoredSection(
            key,
            definition.TitleLocId,
            definition.Title,
            definition.Color,
            true);
        store.Sections.Add(key, section);
        foreach (var childDefinition in definition.Children)
            AddChildSection(section, CreateFactionSection(store, childDefinition));

        return section;
    }

    private static bool HasFactionSection(NpcFactionPrototype prototype)
    {
        return prototype.Name is { } name && !string.IsNullOrEmpty(name.Id) ||
               prototype.Subgroups is { Count: > 0 };
    }

    private RMCGhostFactionSectionDefinition? BuildFactionDefinition(
        ProtoId<NpcFactionPrototype> id,
        Dictionary<ProtoId<NpcFactionPrototype>, NpcFactionPrototype> prototypes,
        Dictionary<ProtoId<NpcFactionPrototype>, RMCGhostFactionSectionDefinition> all,
        Dictionary<ProtoId<NpcFactionPrototype>, ProtoId<NpcFactionPrototype>> childParents,
        Stack<ProtoId<NpcFactionPrototype>> path)
    {
        if (path.Contains(id))
        {
            Log.Error($"Cycle detected in RMC ghost target faction groups: {string.Join(" -> ", path.Reverse())} -> {id}");
            return null;
        }

        if (!prototypes.TryGetValue(id, out var prototype))
        {
            Log.Error($"Unknown RMC ghost target faction subgroup: {id}");
            return null;
        }

        path.Push(id);
        if (!all.TryGetValue(id, out var section))
        {
            section = CreateFactionDefinition(prototype);
            all[id] = section;
        }

        if (prototype.Subgroups is { Count: > 0 })
        {
            foreach (var childId in prototype.Subgroups)
            {
                if (childParents.TryGetValue(childId, out var existingParent))
                {
                    if (existingParent != id)
                    {
                        Log.Error(
                            $"RMC ghost target faction subgroup {childId} has multiple parents: {existingParent} and {id}.");
                    }

                    continue;
                }

                var child = BuildFactionDefinition(childId, prototypes, all, childParents, path);
                if (child == null)
                    continue;

                section.Children.Add(child);
                childParents[childId] = id;
            }
        }

        path.Pop();
        return section;
    }

    private static RMCGhostFactionSectionDefinition CreateFactionDefinition(NpcFactionPrototype prototype)
    {
        if (prototype.Name is { } name &&
            !string.IsNullOrEmpty(name.Id))
        {
            return new RMCGhostFactionSectionDefinition(prototype.ID, name, null, prototype.Color);
        }

        return new RMCGhostFactionSectionDefinition(prototype.ID, EmptyTitle, "-", prototype.Color);
    }

    private static int CompareEntries(
        RMCGhostTargetStoreComponent store,
        RMCGhostTargetStoredEntry a,
        RMCGhostTargetStoredEntry b)
    {
        if (a.SortValue is { } aSort && b.SortValue is { } bSort)
        {
            var sort = bSort.CompareTo(aSort);
            if (sort != 0)
                return sort;
        }
        else if (a.SortValue != null || b.SortValue != null)
        {
            return a.SortValue != null ? -1 : 1;
        }

        var name = string.Compare(
            store.Records[a.Uid].Entry.DisplayName,
            store.Records[b.Uid].Entry.DisplayName,
            StringComparison.CurrentCulture);
        return name != 0
            ? name
            : a.Uid.CompareTo(b.Uid);
    }

    private int CompareSectionsByTitle(
        RMCGhostTargetStoredSection a,
        RMCGhostTargetStoredSection b)
    {
        var aOthers = IsOthersSection(a);
        var bOthers = IsOthersSection(b);
        if (aOthers != bOthers)
            return aOthers ? 1 : -1;

        return string.Compare(GetSectionSortTitle(a), GetSectionSortTitle(b), StringComparison.CurrentCulture);
    }

    private string GetSectionSortTitle(RMCGhostTargetStoredSection section)
    {
        if (!string.IsNullOrEmpty(section.Title))
            return section.Title;

        return string.IsNullOrEmpty(section.TitleLocId.Id)
            ? string.Empty
            : Loc.GetString(section.TitleLocId);
    }

    private static bool IsOthersSection(RMCGhostTargetStoredSection section)
    {
        return section.Key.Kind is RMCGhostTargetSectionKind.Others or RMCGhostTargetSectionKind.MarineOthers;
    }

    private static Color AdjustLightness(Color color, float percent)
    {
        var hsv = Color.ToHsv(color);
        if (percent > 0)
            hsv.Z = Math.Min(hsv.Z * (1f + percent), 1f);
        else
            hsv.Z *= 1f + percent;

        return Color.FromHsv(hsv);
    }

    private void WarpTo(EntityUid uid, EntityUid target)
    {
        _adminLog.Add(LogType.GhostWarp, $"{ToPrettyString(uid)} RMC ghost warped to {ToPrettyString(target)}");

        if ((TryComp(target, out WarpPointComponent? warp) && warp.Follow) ||
            HasComp<MobStateComponent>(target) ||
            _ghostQuery.HasComp(target))
        {
            _follower.StartFollowingEntity(uid, target);
            return;
        }

        var xform = Transform(uid);
        _transform.SetCoordinates(uid, xform, Transform(target).Coordinates);
        _transform.AttachToGridOrMap(uid, xform);
        if (_physicsQuery.TryComp(uid, out var physics))
            _physics.SetLinearVelocity(uid, Vector2.Zero, body: physics);
    }

}
