using System.Diagnostics.CodeAnalysis;
using Content.Server.Power.Components;
using Content.Server.Spawners.Components;
using Content.Shared._RMC14.Item;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.Thunderdome;
using Content.Shared._RMC14.WeedKiller;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Construction.FloorResin;
using Content.Shared._RMC14.Xenonids.Construction.Tunnel;
using Content.Shared.Coordinates;
using Content.Shared.Fax.Components;
using Content.Shared.Roles;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Rules.DistressSignal;

public sealed partial class CMDistressSignalRuleSystem
{
    private const int FaxPowerLoadValue = 5;

    // TODO RMC14: Move these to a prototype
    private string GetRandomOperationName()
    {
        if (_usingCustomOperationName && OperationName != null)
        {
            _usingCustomOperationName = false;
            return OperationName;
        }

        var name = string.Empty;
        if (_operationNames.Count > 0)
            name += $"{_random.Pick(_operationNames)} ";

        if (_operationPrefixes.Count > 0)
            name += $"{_random.Pick(_operationPrefixes)}";

        if (_operationSuffixes.Count > 0)
            name += $"-{_random.Pick(_operationSuffixes)}";

        return name.Trim();
    }

    private void SpawnAdminAreas(CMDistressSignalRuleComponent comp)
    {
        bool SpawnMap(ResPath path, [NotNullWhen(true)] out EntityUid? mapEntityUid)
        {
            mapEntityUid = default;

            try
            {
                if (string.IsNullOrWhiteSpace(path.ToString()))
                    return false;

                if (!_mapLoader.TryLoadMap(path, out var map, out _))
                    return false;

                _mapSystem.InitializeMap((map.Value, map.Value));
                mapEntityUid = map;
                return true;
            }
            catch (Exception e)
            {
                Log.Error($"Error loading {path} map:\n{e}");
            }

            return false;
        }

        foreach (var map in comp.AuxiliaryMaps)
        {
            SpawnMap(new ResPath(map), out _);
        }

        if (SpawnMap(comp.Thunderdome, out var mapEnt))
            EnsureComp<ThunderdomeMapComponent>(mapEnt.Value);
    }

    private void SetCamoType(CamouflageType? ct = null)
    {
        if (ct != null)
        {
            _camo.CurrentMapCamouflage = ct.Value;
            return;
        }

        if (SelectedPlanetMap != null)
            _camo.CurrentMapCamouflage = SelectedPlanetMap.Value.Comp.Camouflage;
    }

    private void SetFriendlyHives(EntityUid hive)
    {
        var query = EntityQueryEnumerator<XenoFriendlyComponent>();
        while (query.MoveNext(out var weeds, out _))
        {
            _hive.SetHive(weeds, hive);
        }

        var resinSlowdown = EntityQueryEnumerator<ResinSlowdownModifierComponent>();
        while (resinSlowdown.MoveNext(out var uid, out _))
        {
            _hive.SetHive(uid, hive);
        }

        var resinSpeedup = EntityQueryEnumerator<ResinSpeedupModifierComponent>();
        while (resinSpeedup.MoveNext(out var uid, out _))
        {
            _hive.SetHive(uid, hive);
        }

        var tunnelQuery = EntityQueryEnumerator<XenoTunnelComponent>();
        var tunnels = new List<EntityUid>();

        while (tunnelQuery.MoveNext(out var ent, out _))
        {
            tunnels.Add(ent);
        }
        
        foreach (var tunnel in tunnels)
        {
            if (_xenoTunnel.TryPlaceTunnel(hive, null, tunnel.ToCoordinates(), out var newTunnel))
                RemCompDeferred<DeletedByWeedKillerComponent>(newTunnel.Value);

            QueueDel(tunnel);
        }
    }

    private void UnpowerFaxes(MapId map)
    {
        var faxes = EntityQueryEnumerator<FaxMachineComponent, ApcPowerReceiverComponent, TransformComponent>();
        while (faxes.MoveNext(out _, out var power, out var xform))
        {
            if (xform.MapID != map)
                continue;

            power.Load = FaxPowerLoadValue;
            power.NeedsPower = true;
        }
    }

    private bool IsChamberFull(EntityUid chamber)
    {
        if (!_hyperSleepChamberQuery.TryComp(chamber, out var hyperSleep))
            return false;

        return _containers.TryGetContainer(chamber, hyperSleep.ContainerId, out var container) &&
               container.Count > 0;
    }

    /// <summary>
    /// Collects and categorizes all squad-based spawn points by squad, job, and availability.
    /// </summary>
    private void CollectSquadSpawners(Spawners spawners)
    {
        var squadQuery = EntityQueryEnumerator<SquadSpawnerComponent>();
        while (squadQuery.MoveNext(out var uid, out var spawner))
        {
            if (IsChamberFull(uid))
            {
                if (spawner.Role == null)
                    spawners.SquadAnyFull.GetOrNew(spawner.Squad).Add(uid);
                else
                    spawners.SquadFull.GetOrNew(spawner.Squad).GetOrNew(spawner.Role.Value).Add(uid);
                continue;
            }

            var found = TryFindAttachedChamber(uid, out var attachedChamber);
            if (found)
            {
                var isFull = IsChamberFull(attachedChamber);
                var target = isFull ? spawners.SquadAnyFull : spawners.SquadAny;
                var targetWithRole = isFull ? spawners.SquadFull : spawners.Squad;

                if (spawner.Role == null)
                    target.GetOrNew(spawner.Squad).Add(attachedChamber);
                else
                    targetWithRole.GetOrNew(spawner.Squad).GetOrNew(spawner.Role.Value).Add(attachedChamber);
            }
            else
            {
                if (spawner.Role == null)
                    spawners.SquadAny.GetOrNew(spawner.Squad).Add(uid);
                else
                    spawners.Squad.GetOrNew(spawner.Squad).GetOrNew(spawner.Role.Value).Add(uid);
            }
        }
    }

    private bool TryFindAttachedChamber(EntityUid spawner, out EntityUid chamber)
    {
        chamber = default;
        foreach (var cardinal in _rmcMap.CardinalDirections)
        {
            var anchored = _rmcMap.GetAnchoredEntitiesEnumerator(spawner, cardinal);
            while (anchored.MoveNext(out var anchoredId))
            {
                if (_hyperSleepChamberQuery.HasComp(anchoredId))
                {
                    chamber = anchoredId;
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Collects and categorizes all non-squad spawn points by job and availability.
    /// </summary>
    private void CollectNonSquadSpawners(Spawners spawners)
    {
        var nonSquadQuery = EntityQueryEnumerator<SpawnPointComponent>();
        while (nonSquadQuery.MoveNext(out var uid, out var spawner))
        {
            if (spawner.Job == null)
                continue;

            var target = IsChamberFull(uid)
                ? spawners.NonSquadFull.GetOrNew(spawner.Job.Value)
                : spawners.NonSquad.GetOrNew(spawner.Job.Value);

            target.Add(uid);
        }
    }

    private Spawners GetSpawners()
    {
        var spawners = new Spawners();
        CollectSquadSpawners(spawners);
        CollectNonSquadSpawners(spawners);
        return spawners;
    }

    private (EntProtoId Id, EntityUid Ent) NextSquad(
        ProtoId<JobPrototype> job,
        CMDistressSignalRuleComponent rule,
        EntProtoId<SquadTeamComponent>? preferred)
    {
        var squads = new List<(EntProtoId SquadId, EntityUid Squad, int Players)>();
        foreach (var (squadId, squad) in rule.Squads)
        {
            var players = 0;
            if (TryComp(squad, out SquadTeamComponent? team))
            {
                var roles = team.Roles;
                var maxRoles = team.MaxRoles;
                if (roles.TryGetValue(job, out var currentPlayers))
                    players = currentPlayers;

                if (preferred != null &&
                    preferred == squadId &&
                    (!maxRoles.TryGetValue(job, out var max) || players < max))
                {
                    return (squadId, squad);
                }
            }

            squads.Add((squadId, squad, players));
        }

        _random.Shuffle(squads);
        squads.Sort((a, b) => a.Players.CompareTo(b.Players));

        var chosen = squads[0];
        return (chosen.SquadId, chosen.Squad);
    }

    /// <summary>
    /// Finds the best spawn point for a player based on their job, squad preference, and availability.
    /// Falls back to generic spawn points if preferred ones are unavailable.
    /// </summary>
    private (EntityUid Spawner, EntityUid? Squad)? GetSpawner(
        CMDistressSignalRuleComponent rule,
        JobPrototype job,
        EntProtoId<SquadTeamComponent>? preferred)
    {
        var allSpawners = GetSpawners();
        EntityUid? squad = null;

        if (job.HasSquad)
        {
            var (squadId, squadEnt) = NextSquad(job, rule, preferred);
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
                if (point.Job?.Id == job.ID)
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

    private void ReloadPrototypes()
    {
        _operationNames.Clear();
        _operationPrefixes.Clear();
        _operationSuffixes.Clear();

        foreach (var prototype in _prototypes.EnumeratePrototypes<EntityPrototype>())
        {
            if (prototype.TryGetComponent(out RMCDistressSignalNamesComponent? names, _compFactory))
                _operationNames.UnionWith(names.Names);

            if (prototype.TryGetComponent(out RMCDistressSignalPrefixesComponent? prefixes, _compFactory))
                _operationPrefixes.UnionWith(prefixes.Prefixes);

            if (prototype.TryGetComponent(out RMCDistressSignalSuffixesComponent? suffixes, _compFactory))
                _operationSuffixes.UnionWith(suffixes.Suffixes);
        }
    }

    /// <summary>
    /// Container class for organizing spawn points by category (squad vs. non-squad, full vs. available).
    /// Used to efficiently manage and query spawn point availability during player spawning.
    /// </summary>
    private sealed class Spawners
    {
        // Squad spawners with available slots, organized by squad and job
        public readonly Dictionary<EntProtoId, Dictionary<ProtoId<JobPrototype>, List<EntityUid>>> Squad = new();
        // Squad spawners with any job slot, organized by squad
        public readonly Dictionary<EntProtoId, List<EntityUid>> SquadAny = new();
        // Full squad spawners with available slots, organized by squad and job
        public readonly Dictionary<EntProtoId, Dictionary<ProtoId<JobPrototype>, List<EntityUid>>> SquadFull = new();
        // Full squad spawners with any job slot, organized by squad
        public readonly Dictionary<EntProtoId, List<EntityUid>> SquadAnyFull = new();
        // Non-squad spawners with available slots, organized by job
        public readonly Dictionary<ProtoId<JobPrototype>, List<EntityUid>> NonSquad = new();
        // Full non-squad spawners, organized by job
        public readonly Dictionary<ProtoId<JobPrototype>, List<EntityUid>> NonSquadFull = new();
    }
}
