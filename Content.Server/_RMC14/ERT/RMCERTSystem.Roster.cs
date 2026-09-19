using System.Linq;
using System.Numerics;
using System.Text;
using Content.Server._RMC14.Dropship;
using Content.Server._RMC14.Rules.DistressSignal;
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

public sealed partial class RMCERTSystem
{

    private bool BuildRoster(RMCERTRequest request, RMCERTCallPrototype call, out string error)
    {
        error = string.Empty;
        // Required slots are guaranteed first, then optional slots fill out the SS13 mob_max equivalent.
        var roles = call.Roles
            .OrderByDescending(r => r.Leader)
            .ThenByDescending(r => r.Priority)
            .ToList();
        var countsByRole = new Dictionary<string, int>();
        var minTotal = 0;
        var maxTotal = 0;

        foreach (var role in roles)
        {
            if (role.GhostRoleEntityPool.Count == 0)
            {
                error = Loc.GetString("rmc-ert-error-empty-ghost-role-pool",
                    ("role", role.Id),
                    ("call", call.Name));
                Log.Error($"ERT call {call.ID} role {role.Id} has no ghost role entity pool entries.");
                return false;
            }

            foreach (var entry in role.GhostRoleEntityPool)
            {
                if (entry.Weight <= 0)
                {
                    error = Loc.GetString("rmc-ert-error-invalid-ghost-role-pool-weight",
                        ("entity", entry.Entity.Id),
                        ("role", role.Id),
                        ("call", call.Name),
                        ("weight", entry.Weight));
                    Log.Error($"ERT call {call.ID} role {role.Id} has invalid ghost role entity pool weight {entry.Weight} for {entry.Entity}.");
                    return false;
                }

                if (_prototypes.HasIndex<EntityPrototype>(entry.Entity))
                    continue;

                error = Loc.GetString("rmc-ert-error-missing-ghost-role",
                    ("entity", entry.Entity.Id),
                    ("call", call.Name));
                return false;
            }

            var min = GetRoleMinimumCount(role);
            var max = GetRoleMaximumCount(role);
            countsByRole[role.Id] = 0;
            minTotal += min;
            maxTotal += max;

            for (var i = 0; i < min; i++)
            {
                if (!TryAddRosterSlot(request, call, role, out error))
                    return false;

                countsByRole[role.Id]++;
            }
        }

        var targetMin = Math.Max(minTotal, call.Requirements.MinRequiredSlots);
        var targetMax = maxTotal;
        if (call.Requirements.MaxSlots > 0)
            targetMax = Math.Min(targetMax, call.Requirements.MaxSlots);

        if (targetMin > targetMax)
        {
            error = Loc.GetString("rmc-ert-error-min-slots-over-max",
                ("call", call.Name),
                ("required", targetMin),
                ("maximum", targetMax));
            return false;
        }

        var targetCount = targetMax;

        while (request.PlannedRoster.Count < targetCount)
        {
            var totalWeight = 0;
            foreach (var role in roles)
            {
                var remaining = GetRoleMaximumCount(role) - countsByRole[role.Id];
                if (remaining > 0)
                    totalWeight += remaining;
            }

            if (totalWeight <= 0)
                break;

            var roll = _random.Next(totalWeight);
            RMCERTRoleEntry? selected = null;
            foreach (var role in roles)
            {
                var remaining = GetRoleMaximumCount(role) - countsByRole[role.Id];
                if (remaining <= 0)
                    continue;

                if (roll < remaining)
                {
                    selected = role;
                    break;
                }

                roll -= remaining;
            }

            if (selected == null)
                break;

            if (!TryAddRosterSlot(request, call, selected, out error))
                return false;

            countsByRole[selected.Id]++;
        }

        if (request.PlannedRoster.Count < targetMin)
        {
            error = Loc.GetString("rmc-ert-error-planned-slots-too-low",
                ("planned", request.PlannedRoster.Count),
                ("required", targetMin));
            return false;
        }

        return request.PlannedRoster.Count > 0;
    }

    private bool SpawnRosterSlots(RMCERTRequest request, RMCERTCallPrototype call, EntityUid? shuttle, out string error)
    {
        error = string.Empty;

        foreach (var slot in request.PlannedRoster)
        {
            var coords = GetSpawnCoordinates(request, shuttle, slot);
            var spawned = SpawnResponseMember(request, call, slot, coords);
            TryAssignSeat(spawned, shuttle, slot);
            request.SpawnedGhostRoles.Add(spawned);
        }

        return request.SpawnedGhostRoles.Count > 0;
    }

    private bool SpawnShuttleCargo(RMCERTRequest request, RMCERTCallPrototype call, EntityUid? shuttle, out string error)
    {
        error = string.Empty;
        if (shuttle is not { Valid: true } shuttleUid || !Exists(shuttleUid))
        {
            error = Loc.GetString("rmc-ert-error-no-shuttle", ("call", call.Name));
            return false;
        }

        if (call.ShuttleCargo.Count == 0)
        {
            error = Loc.GetString("rmc-ert-error-missing-shuttle-cargo", ("call", call.Name));
            return false;
        }

        var cargoCount = call.ShuttleCargoCount > 0
            ? call.ShuttleCargoCount
            : call.ShuttleCargo.Count;
        var coordinates = GetShuttleCargoCoordinates(shuttleUid);
        var cargoPool = call.ShuttleCargo.ToList();
        var coordinatePool = coordinates.ToList();
        var spawnedCargo = new List<EntityUid>();

        for (var i = 0; i < cargoCount; i++)
        {
            if (cargoPool.Count == 0)
                cargoPool = call.ShuttleCargo.ToList();
            if (coordinatePool.Count == 0)
                coordinatePool = coordinates.ToList();

            var cargoIndex = _random.Next(cargoPool.Count);
            var cargo = cargoPool[cargoIndex];
            cargoPool.RemoveAt(cargoIndex);

            var coordinateIndex = _random.Next(coordinatePool.Count);
            var coords = coordinatePool[coordinateIndex];
            coordinatePool.RemoveAt(coordinateIndex);

            spawnedCargo.Add(Spawn(cargo, coords));
        }

        AddERTRequestLog(LogImpact.Medium,
            "cargo spawned",
            request,
            "system",
            call,
            $"count={spawnedCargo.Count}, entities=[{string.Join(", ", spawnedCargo.Select(uid => ToPrettyString(uid)))}]");
        return spawnedCargo.Count > 0;
    }

    private List<EntityCoordinates> GetShuttleCargoCoordinates(EntityUid shuttle)
    {
        var coordinates = new List<EntityCoordinates>();
        var query = EntityQueryEnumerator<RMCERTSpawnPointComponent, TransformComponent>();
        while (query.MoveNext(out _, out _, out var xform))
        {
            if (xform.GridUid == shuttle)
                coordinates.Add(xform.Coordinates);
        }

        if (coordinates.Count == 0)
            coordinates.Add(new EntityCoordinates(shuttle, Vector2.Zero));

        return coordinates;
    }

    private bool TryAddRosterSlot(
        RMCERTRequest request,
        RMCERTCallPrototype call,
        RMCERTRoleEntry role,
        out string error)
    {
        if (!TryPickGhostRoleEntity(call, role, out var ghostRoleEntity, out error))
            return false;

        request.PlannedRoster.Add(new RMCERTRosterSlot
        {
            RoleId = role.Id,
            RoleName = role.Name,
            GhostRoleEntity = ghostRoleEntity,
            Leader = role.Leader,
            Priority = role.Priority,
            RoleTags = role.RoleTags.ToList(),
            SeatTags = role.SeatTags.ToList(),
        });

        return true;
    }

    private bool TryPickGhostRoleEntity(
        RMCERTCallPrototype call,
        RMCERTRoleEntry role,
        out EntProtoId ghostRoleEntity,
        out string error)
    {
        ghostRoleEntity = default;
        error = string.Empty;

        if (role.GhostRoleEntityPool.Count == 0)
        {
            error = Loc.GetString("rmc-ert-error-empty-ghost-role-pool",
                ("role", role.Id),
                ("call", call.Name));
            Log.Error($"ERT call {call.ID} role {role.Id} has no ghost role entity pool entries.");
            return false;
        }

        var totalWeight = 0;
        foreach (var entry in role.GhostRoleEntityPool)
            totalWeight += Math.Max(0, entry.Weight);

        if (totalWeight <= 0)
        {
            error = Loc.GetString("rmc-ert-error-empty-ghost-role-pool",
                ("role", role.Id),
                ("call", call.Name));
            Log.Error($"ERT call {call.ID} role {role.Id} has no positive ghost role entity pool weights.");
            return false;
        }

        var roll = _random.Next(totalWeight);
        foreach (var entry in role.GhostRoleEntityPool)
        {
            var weight = Math.Max(0, entry.Weight);
            if (weight == 0)
                continue;

            if (roll < weight)
            {
                ghostRoleEntity = entry.Entity;
                return true;
            }

            roll -= weight;
        }

        ghostRoleEntity = role.GhostRoleEntityPool[^1].Entity;
        return true;
    }

    private EntityUid SpawnResponseMember(
        RMCERTRequest request,
        RMCERTCallPrototype call,
        RMCERTRosterSlot slot,
        EntityCoordinates coordinates)
    {
        EntityUid spawned;
        if (_prototypes.TryIndex(slot.GhostRoleEntity, out var entityPrototype) &&
            entityPrototype.TryGetComponent<RandomHumanoidSpawnerComponent>(out var spawner, _componentFactory) &&
            !string.IsNullOrWhiteSpace(spawner.SettingsPrototypeId))
        {
            spawned = _randomHumanoid.SpawnRandomHumanoid(spawner.SettingsPrototypeId, coordinates, slot.RoleName);
        }
        else
        {
            spawned = Spawn(slot.GhostRoleEntity, coordinates);
        }

        if (TryComp(spawned, out GhostRoleComponent? ghostRole))
        {
            ghostRole.MindRoles.Clear();
        }

        var member = EnsureComp<RMCERTMemberComponent>(spawned);
        member.RequestId = request.Id;
        member.Call = call.ID;
        member.Role = slot.RoleId;
        member.Team = call.Name;
        Dirty(spawned, member);

        return spawned;
    }

    private bool TryAssignSeat(EntityUid member, EntityUid? shuttle, RMCERTRosterSlot slot)
    {
        if (shuttle is not { Valid: true } shuttleUid)
            return false;

        // Prefer the highest-priority compatible seat so command and specialist slots claim their reserved positions first.
        var bestSeat = EntityUid.Invalid;
        var bestPriority = int.MinValue;
        var query = EntityQueryEnumerator<RMCERTSeatComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var seat, out var xform))
        {
            if (xform.GridUid != shuttleUid)
                continue;

            if (seat.OccupiedBy != null)
                continue;

            var roleMatch = MatchesAny(seat.ReservedRoleTags, slot.RoleTags);
            var seatMatch = MatchesAny(seat.SeatTags, slot.SeatTags);
            if (!roleMatch && !seatMatch)
                continue;

            if (seat.Priority <= bestPriority)
                continue;

            bestSeat = uid;
            bestPriority = seat.Priority;
        }

        if (!bestSeat.Valid)
            return false;

        var bestSeatComp = Comp<RMCERTSeatComponent>(bestSeat);
        bestSeatComp.OccupiedBy = GetNetEntity(member);
        bestSeatComp.ReservationExpires = null;
        Dirty(bestSeat, bestSeatComp);

        var coordinates = Transform(bestSeat).Coordinates;
        _transform.SetCoordinates(member, coordinates);
        _buckle.TryBuckle(member, null, bestSeat, popup: false);
        return true;
    }

    private EntityCoordinates GetSpawnCoordinates(RMCERTRequest request, EntityUid? shuttle, RMCERTRosterSlot slot)
    {
        if (shuttle is { Valid: true } shuttleUid)
        {
            // Match SS13-style landmark behavior by picking randomly among the best compatible spawn markers.
            var matchingSpawns = new List<EntityUid>();
            var highestPriority = int.MinValue;
            var query = EntityQueryEnumerator<RMCERTSpawnPointComponent, TransformComponent>();
            while (query.MoveNext(out var uid, out var spawn, out var xform))
            {
                if (xform.GridUid != shuttleUid)
                    continue;

                if (!MatchesAny(spawn.RoleTags, slot.RoleTags) && !MatchesAny(spawn.SeatTags, slot.SeatTags))
                    continue;

                if (spawn.Priority > highestPriority)
                {
                    highestPriority = spawn.Priority;
                    matchingSpawns.Clear();
                }

                if (spawn.Priority == highestPriority)
                    matchingSpawns.Add(uid);
            }

            if (matchingSpawns.Count > 0)
                return Transform(_random.Pick(matchingSpawns)).Coordinates;

            return new EntityCoordinates(shuttleUid, Vector2.Zero);
        }

        if (request.SourceEntity is { Valid: true } source &&
            TryComp(source, out TransformComponent? sourceXform))
        {
            return sourceXform.Coordinates;
        }

        return new EntityCoordinates(EntityUid.Invalid, Vector2.Zero);
    }

    private static int GetRoleMinimumCount(RMCERTRoleEntry role)
    {
        var min = Math.Max(0, role.Min);
        if (role.Required)
            min = Math.Max(min, 1);

        return min;
    }

    private static int GetRoleMaximumCount(RMCERTRoleEntry role)
    {
        var min = GetRoleMinimumCount(role);
        return Math.Max(min, role.Max);
    }
}
