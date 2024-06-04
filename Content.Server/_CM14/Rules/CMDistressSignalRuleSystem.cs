using System.Runtime.InteropServices;
using Content.Server._CM14.Marines;
using Content.Server.Administration.Components;
using Content.Server.Administration.Managers;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Components;
using Content.Server.GameTicking.Rules;
using Content.Server.Mind;
using Content.Server.Parallax;
using Content.Server.Players.PlayTimeTracking;
using Content.Server.Power.Components;
using Content.Server.Preferences.Managers;
using Content.Server.RoundEnd;
using Content.Server.Shuttles.Systems;
using Content.Server.Spawners.Components;
using Content.Server.Spawners.EntitySystems;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared._CM14.Marines;
using Content.Shared._CM14.Marines.HyperSleep;
using Content.Shared._CM14.Marines.Squads;
using Content.Shared._CM14.Weapons.Ranged.IFF;
using Content.Shared._CM14.Xenos;
using Content.Shared.Coordinates;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.StatusIcon;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server._CM14.Rules;

public sealed class CMDistressSignalRuleSystem : GameRuleSystem<CMDistressSignalRuleComponent>
{
    [Dependency] private readonly IBanManager _bans = default!;
    [Dependency] private readonly BiomeSystem _biome = default!;
    [Dependency] private readonly ContainerSystem _containers = default!;
    [Dependency] private readonly GunIFFSystem _gunIFF = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly MarineSystem _marines = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly PlayTimeTrackingSystem _playTime = default!;
    [Dependency] private readonly IServerPreferencesManager _prefsManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;
    [Dependency] private readonly SquadSystem _squad = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;

    [ValidatePrototypeId<BiomeTemplatePrototype>]
    private const string PlanetBiome = "Grasslands";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RulePlayerSpawningEvent>(OnRulePlayerSpawning);
        SubscribeLocalEvent<PlayerSpawningEvent>(OnPlayerSpawning,
            before: [typeof(ArrivalsSystem), typeof(SpawnPointSystem)]);

        SubscribeLocalEvent<MarineComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<MarineComponent, ComponentRemove>(OnCompRemove);

        SubscribeLocalEvent<XenoComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<XenoComponent, ComponentRemove>(OnCompRemove);

        SubscribeLocalEvent<AlmayerComponent, MapInitEvent>(OnAlmayerMapInit);
    }

    private void OnRulePlayerSpawning(RulePlayerSpawningEvent ev)
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out var uid, out _, out var comp, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;

            comp.Hive = Spawn(comp.HiveId);
            if (!SpawnXenoMap((uid, comp)))
            {
                Log.Error("Failed to load xeno map");
                continue;
            }

            bool IsAllowed(NetUserId id, ProtoId<JobPrototype> role)
            {
                if (!_player.TryGetSessionById(id, out var player))
                    return false;

                var jobBans = _bans.GetJobBans(player.UserId);
                if (jobBans == null || jobBans.Contains(role))
                    return false;

                if (!_playTime.IsAllowed(player, role))
                    return false;

                return true;
            }

            NetUserId? SpawnXeno(List<NetUserId> list, EntProtoId ent)
            {
                var playerId = _random.PickAndTake(list);
                if (!_player.TryGetSessionById(playerId, out var player))
                {
                    Log.Error($"Failed to find player with id {playerId} during xeno selection.");
                    return null;
                }

                ev.PlayerPool.Remove(player);
                GameTicker.PlayerJoinGame(player);

                // TODO CM14 xeno spawn points
                var xenoEnt = Spawn(ent, comp.XenoMap.ToCoordinates());

                _xeno.MakeXeno(xenoEnt);
                _xeno.SetHive(xenoEnt, comp.Hive);

                if (!_mind.TryGetMind(playerId, out var mind))
                    mind = _mind.CreateMind(playerId);

                _mind.TransferTo(mind.Value, xenoEnt);
                return playerId;
            }

            var totalXenos = Math.Max(1, ev.PlayerPool.Count / comp.PlayersPerXeno);
            var xenoCandidates = new List<NetUserId>[Enum.GetValues<JobPriority>().Length];
            for (var i = 0; i < xenoCandidates.Length; i++)
            {
                xenoCandidates[i] = [];
            }

            foreach (var (id, profile) in ev.Profiles)
            {
                if (!IsAllowed(id, comp.QueenJob))
                    continue;

                if (profile.JobPriorities.TryGetValue(comp.QueenJob, out var priority) &&
                    priority > JobPriority.Never)
                {
                    xenoCandidates[(int) priority].Add(id);
                }
            }

            NetUserId? queenSelected = null;
            for (var i = xenoCandidates.Length - 1; i >= 0; i--)
            {
                var list = xenoCandidates[i];
                while (list.Count > 0)
                {
                    queenSelected = SpawnXeno(list, comp.QueenEnt);
                    if (queenSelected != null)
                        break;
                }

                if (queenSelected != null)
                {
                    totalXenos--;
                    break;
                }
            }

            foreach (var list in xenoCandidates)
            {
                list.Clear();
            }

            foreach (var (id, profile) in ev.Profiles)
            {
                if (id == queenSelected)
                    continue;

                if (!IsAllowed(id, comp.XenoSelectableJob))
                    continue;

                if (profile.JobPriorities.TryGetValue(comp.XenoSelectableJob, out var priority) &&
                    priority > JobPriority.Never)
                {
                    xenoCandidates[(int) priority].Add(id);
                }
            }

            var selected = 0;
            for (var i = xenoCandidates.Length - 1; i >= 0; i--)
            {
                var list = xenoCandidates[i];
                while (list.Count > 0 && selected < totalXenos)
                {
                    if (SpawnXeno(list, comp.LarvaEnt) != null)
                        selected++;
                }
            }

            for (var i = selected; i < totalXenos; i++)
            {
                // TODO CM14 xeno spawn points
                var xenoEnt = Spawn(comp.LarvaEnt, comp.XenoMap.ToCoordinates());
                _xeno.MakeXeno(xenoEnt);
                _xeno.SetHive(xenoEnt, comp.Hive);
            }
        }
    }

    private void OnPlayerSpawning(PlayerSpawningEvent ev)
    {
        if (ev.Job?.Prototype is not { } jobId ||
            !_prototypes.TryIndex(jobId, out var job) ||
            !job.IsCM)
        {
            return;
        }

        var query = QueryActiveRules();
        while (query.MoveNext(out _, out _, out var comp, out _))
        {
            if (GetSpawner(comp, job) is not { } spawnerInfo)
                return;

            var (spawner, squad) = spawnerInfo;
            if (TryComp(spawner, out HyperSleepChamberComponent? hyperSleep) &&
                _containers.TryGetContainer(spawner, hyperSleep.ContainerId, out var container))
            {
                ev.SpawnResult = _stationSpawning.SpawnPlayerMob(spawner.ToCoordinates(), ev.Job, ev.HumanoidCharacterProfile, ev.Station);
                _containers.Insert(ev.SpawnResult.Value, container);
            }
            else
            {
                var coordinates = _transform.GetMoverCoordinates(spawner);
                ev.SpawnResult = _stationSpawning.SpawnPlayerMob(coordinates, ev.Job, ev.HumanoidCharacterProfile, ev.Station);
            }

            // TODO CM14 split this out with an event
            SpriteSpecifier? icon = null;
            if (job.HasIcon && _prototypes.TryIndex(job.Icon, out StatusIconPrototype? jobIcon))
                icon = jobIcon.Icon;

            _marines.MakeMarine(ev.SpawnResult.Value, icon);

            if (squad != null)
            {
                _squad.AssignSquad(ev.SpawnResult.Value, squad.Value, ev.Job);

                // TODO CM14 add this to the map file
                if (TryComp(spawner, out TransformComponent? xform) &&
                    xform.GridUid != null)
                {
                    EnsureComp<AlmayerComponent>(xform.GridUid.Value);
                }
            }

            if (TryComp(ev.SpawnResult, out HungerComponent? hunger))
                _hunger.SetHunger(ev.SpawnResult.Value, 50.0f, hunger);

            _gunIFF.SetUserFaction(ev.SpawnResult.Value, comp.MarineFaction);

            return;
        }
    }

    private void OnMobStateChanged<T>(Entity<T> ent, ref MobStateChangedEvent args) where T : IComponent?
    {
        if (args.NewMobState == MobState.Dead)
            CheckRoundShouldEnd();
    }

    private void OnCompRemove<T>(Entity<T> ent, ref ComponentRemove args) where T : IComponent?
    {
        CheckRoundShouldEnd();
    }

    private void OnAlmayerMapInit(Entity<AlmayerComponent> almayer, ref MapInitEvent args)
    {
        foreach (var ent in GetChildren(almayer))
        {
            if (!HasComp<StationInfiniteBatteryTargetComponent>(ent))
                continue;

            var recharger = EnsureComp<BatterySelfRechargerComponent>(ent);
            var battery = EnsureComp<BatteryComponent>(ent);

            recharger.AutoRecharge = true;
            recharger.AutoRechargeRate = battery.MaxCharge; // Instant refill.
        }
    }

    protected override void OnStartAttempt(Entity<CMDistressSignalRuleComponent, GameRuleComponent> gameRule, RoundStartAttemptEvent ev)
    {
        if (ev.Forced || ev.Cancelled)
            return;

        var query = QueryAllRules();
        while (query.MoveNext(out _, out var distress, out _))
        {
            var xenoCandidate = false;
            foreach (var player in ev.Players)
            {
                if (_prefsManager.TryGetCachedPreferences(player.UserId, out var preferences))
                {
                    var profile = (HumanoidCharacterProfile) preferences.GetProfile(preferences.SelectedCharacterIndex);
                    if (profile.JobPriorities.TryGetValue(distress.XenoSelectableJob, out var xenoPriority) &&
                        xenoPriority > JobPriority.Never)
                    {
                        xenoCandidate = true;
                        break;
                    }

                    if (profile.JobPriorities.TryGetValue(distress.QueenJob, out var queenPriority) &&
                        queenPriority > JobPriority.Never)
                    {
                        xenoCandidate = true;
                        break;
                    }
                }
            }

            if (xenoCandidate)
                continue;

            ChatManager.SendAdminAnnouncement("Can't start distress signal. Requires at least 1 xeno player but we have 0.");
            ev.Cancel();
        }
    }

    private IEnumerable<EntityUid> GetChildren(EntityUid almayer)
    {
        if (TryComp<StationDataComponent>(almayer, out var station))
        {
            foreach (var grid in station.Grids)
            {
                var enumerator = Transform(grid).ChildEnumerator;
                while (enumerator.MoveNext(out var ent))
                {
                    yield return ent;
                }
            }
        }
        else if (HasComp<MapComponent>(almayer))
        {
            var enumerator = Transform(almayer).ChildEnumerator;
            while (enumerator.MoveNext(out var possibleGrid))
            {
                var enumerator2 = Transform(possibleGrid).ChildEnumerator;
                while (enumerator2.MoveNext(out var ent))
                {
                    yield return ent;
                }
            }
        }
        else
        {
            var enumerator = Transform(almayer).ChildEnumerator;
            while (enumerator.MoveNext(out var ent))
            {
                yield return ent;
            }
        }
    }

    private void CheckRoundShouldEnd()
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out var uid, out _, out var distress, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;

            var xenos = EntityQueryEnumerator<XenoComponent, MobStateComponent, TransformComponent>();
            var xenosAlive = false;
            var xenosOnShip = false;
            while (xenos.MoveNext(out var xenoId, out var xeno, out var mobState, out var xform))
            {
                if (!xeno.ContributesToVictory)
                    continue;

                if (_mobState.IsAlive(xenoId, mobState))
                    xenosAlive = true;

                if (HasComp<AlmayerComponent>(xform.GridUid))
                    xenosOnShip = true;

                if (xenosAlive && xenosOnShip)
                    break;
            }

            var marines = EntityQueryEnumerator<MarineComponent, MobStateComponent, TransformComponent>();
            var marinesAlive = false;
            var marinesOnShip = false;
            while (marines.MoveNext(out var marineId, out _, out var mobState, out var xform))
            {
                if (_mobState.IsAlive(marineId, mobState))
                    marinesAlive = true;

                if (HasComp<AlmayerComponent>(xform.GridUid))
                    marinesOnShip = true;

                if (marinesAlive && marinesOnShip)
                    break;
            }

            if (xenosOnShip)
                distress.XenosEverOnShip = true;

            if (!xenosAlive && !marinesAlive)
            {
                _roundEnd.EndRound();
                return;
            }

            if (distress.XenosEverOnShip)
            {
                if (xenosAlive && !marinesOnShip)
                {
                    // TODO CM14 major xeno victory
                    _roundEnd.EndRound();
                    return;
                }

                if (!xenosAlive || !xenosOnShip)
                {
                    // TODO CM14 minor xeno victory
                    _roundEnd.EndRound();
                    return;
                }
            }
            else
            {
                if (xenosAlive && !marinesAlive)
                {
                    // TODO CM14 major xeno victory
                    _roundEnd.EndRound();
                    return;
                }

                if (!xenosAlive && marinesAlive)
                {
                    // TODO CM14 major marine victory
                    _roundEnd.EndRound();
                    return;
                }
            }

            // TODO CM14 no queen minor marine victory
        }
    }

    private bool SpawnXenoMap(Entity<CMDistressSignalRuleComponent> rule)
    {
        var mapId = _map.CreateMap();
        _biome.EnsurePlanet(mapId, _prototypes.Index<BiomeTemplatePrototype>(PlanetBiome));
        rule.Comp.XenoMap = mapId;
        return true;
    }

    private Spawners GetSpawners()
    {
        var spawners = new Spawners();
        var squadQuery = EntityQueryEnumerator<SquadSpawnerComponent>();
        while (squadQuery.MoveNext(out var uid, out var spawner))
        {
            if (TryComp(uid, out HyperSleepChamberComponent? hyperSleep) &&
                _containers.TryGetContainer(uid, hyperSleep.ContainerId, out var container) &&
                container.Count > 0)
            {
                if (spawner.Role == null)
                    spawners.SquadAnyFull.GetOrNew(spawner.Squad).Add(uid);
                else
                    spawners.SquadFull.GetOrNew(spawner.Squad).GetOrNew(spawner.Role.Value).Add(uid);
            }
            else
            {
                if (spawner.Role == null)
                    spawners.SquadAny.GetOrNew(spawner.Squad).Add(uid);
                else
                    spawners.Squad.GetOrNew(spawner.Squad).GetOrNew(spawner.Role.Value).Add(uid);
            }
        }

        var nonSquadQuery = EntityQueryEnumerator<SpawnPointComponent>();
        while (nonSquadQuery.MoveNext(out var uid, out var spawner))
        {
            if (spawner.Job == null)
                continue;

            if (TryComp(uid, out HyperSleepChamberComponent? hyperSleep) &&
                _containers.TryGetContainer(uid, hyperSleep.ContainerId, out var container) &&
                container.Count > 0)
            {
                spawners.NonSquadFull.GetOrNew(spawner.Job.ID).Add(uid);
            }
            else
            {
                spawners.NonSquad.GetOrNew(spawner.Job.ID).Add(uid);
            }
        }

        return spawners;
    }

    private (EntProtoId Id, EntityUid Ent) NextSquad(CMDistressSignalRuleComponent rule)
    {
        if (rule.NextSquad >= rule.SquadIds.Count)
            rule.NextSquad = 0;

        var id = rule.SquadIds[rule.NextSquad++];
        ref var squad = ref CollectionsMarshal.GetValueRefOrAddDefault(rule.Squads, id, out var exists);
        if (!exists)
            squad = Spawn(id);

        return (id, squad);
    }

    private (EntityUid Spawner, EntityUid? Squad)? GetSpawner(CMDistressSignalRuleComponent rule, JobPrototype job)
    {
        var allSpawners = GetSpawners();
        EntityUid? squad = null;

        if (job.HasSquad)
        {
            var (squadId, squadEnt) = NextSquad(rule);
            squad = squadEnt;

            if (allSpawners.Squad.TryGetValue(squadId, out var jobSpawners) &&
                jobSpawners.TryGetValue(job.ID, out var spawners))
            {
                return (_random.Pick(spawners), squadEnt);
            }

            if (allSpawners.SquadAny.TryGetValue(squadId, out var anySpawners))
                return (_random.Pick(anySpawners), squadEnt);

            if (allSpawners.SquadFull.TryGetValue(squadId, out jobSpawners) &&
                jobSpawners.TryGetValue(job.ID, out spawners))
            {
                return (_random.Pick(spawners), squadEnt);
            }

            if (allSpawners.SquadAnyFull.TryGetValue(squadId, out anySpawners))
                return (_random.Pick(anySpawners), squadEnt);

            Log.Error($"No valid spawn found for player. Squad: {squadId}, job: {job.ID}");

            if (allSpawners.NonSquad.TryGetValue(job.ID, out spawners))
                return (_random.Pick(spawners), squadEnt);

            if (allSpawners.NonSquadFull.TryGetValue(job.ID, out spawners))
                return (_random.Pick(spawners), squadEnt);

            Log.Error($"No valid spawn found for player. Job: {job.ID}");
        }
        else
        {
            if (allSpawners.NonSquad.TryGetValue(job.ID, out var spawners))
                return (_random.Pick(spawners), null);

            if (allSpawners.NonSquadFull.TryGetValue(job.ID, out spawners))
                return (_random.Pick(spawners), null);

            Log.Error($"No valid spawn found for player. Job: {job.ID}");
        }

        var pointsQuery = EntityQueryEnumerator<SpawnPointComponent>();
        var jobPoints = new List<EntityUid>();
        var anyJobPoints = new List<EntityUid>();
        var latePoints = new List<EntityUid>();

        while (pointsQuery.MoveNext(out var uid, out var point))
        {
            if (point.SpawnType == SpawnPointType.Job)
            {
                if (point.Job?.ID == job.ID)
                    jobPoints.Add(uid);
                else
                    anyJobPoints.Add(uid);
            }

            if (point.SpawnType == SpawnPointType.LateJoin)
                latePoints.Add(uid);
        }

        if (jobPoints.Count > 0)
            return (_random.Pick(jobPoints), squad);

        if (anyJobPoints.Count > 0)
            return (_random.Pick(anyJobPoints), squad);

        if (latePoints.Count > 0)
            return (_random.Pick(latePoints), squad);

        return null;
    }
}

public sealed class Spawners
{
    public readonly Dictionary<EntProtoId, Dictionary<ProtoId<JobPrototype>, List<EntityUid>>> Squad = new();
    public readonly Dictionary<EntProtoId, List<EntityUid>> SquadAny = new();
    public readonly Dictionary<EntProtoId, Dictionary<ProtoId<JobPrototype>, List<EntityUid>>> SquadFull = new();
    public readonly Dictionary<EntProtoId, List<EntityUid>> SquadAnyFull = new();
    public readonly Dictionary<ProtoId<JobPrototype>, List<EntityUid>> NonSquad = new();
    public readonly Dictionary<ProtoId<JobPrototype>, List<EntityUid>> NonSquadFull = new();
}
