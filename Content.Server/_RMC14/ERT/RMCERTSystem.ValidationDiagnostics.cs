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

    private void ValidatePrototypes()
    {
        foreach (var call in _prototypes.EnumeratePrototypes<RMCERTCallPrototype>())
        {
            if (call.Roles.Count == 0 &&
                !IsCargoOnlyShuttle(call))
            {
                Log.Error($"ERT call {call.ID} has no roles configured.");
            }

            if (!TryGetShuttleMapPath(call, out _, out var shuttleError))
                Log.Error($"ERT call {call.ID} has invalid shuttle configuration: {shuttleError}");

            if (call.ShuttleCargo.Count > 0 || call.ShuttleCargoCount > 0)
            {
                if (call.ShuttleCargo.Count == 0)
                    Log.Error($"ERT call {call.ID} has shuttleCargoCount but no shuttle cargo prototypes.");

                foreach (var cargo in call.ShuttleCargo)
                {
                    if (!_prototypes.HasIndex<EntityPrototype>(cargo))
                        Log.Error($"ERT call {call.ID} references missing shuttle cargo entity {cargo}.");
                }
            }

            if (call.ShuttleSpawnMarker is { } spawnMarker)
            {
                if (!_prototypes.TryIndex(spawnMarker, out var spawnMarkerPrototype))
                {
                    Log.Error($"ERT call {call.ID} references missing shuttle spawn marker {spawnMarker.Id}.");
                }
                else if (!spawnMarkerPrototype.HasComponent<RMCERTShuttleStartPadComponent>(_componentFactory) &&
                         !spawnMarkerPrototype.HasComponent<GridSpawnerComponent>(_componentFactory))
                {
                    Log.Error($"ERT call {call.ID} shuttle spawn marker {spawnMarker.Id} is missing RMCERTShuttleStartPadComponent.");
                }
            }

            if (string.IsNullOrWhiteSpace(call.Organization) &&
                call.NpcFactions.Count == 0 &&
                call.IffFaction == null)
            {
                Log.Error($"ERT call {call.ID} is missing organization, NPC factions, and IFF faction metadata.");
            }

            foreach (var role in call.Roles)
            {
                if (role.GhostRoleEntityPool.Count == 0)
                    Log.Error($"ERT call {call.ID} role {role.Id} has no ghost role entity pool entries.");

                foreach (var entry in role.GhostRoleEntityPool)
                {
                    if (entry.Weight <= 0)
                        Log.Error($"ERT call {call.ID} role {role.Id} has non-positive ghost role entity pool weight {entry.Weight} for {entry.Entity}.");

                    if (!_prototypes.HasIndex<EntityPrototype>(entry.Entity))
                        Log.Error($"ERT call {call.ID} role {role.Id} references missing pooled ghost role entity {entry.Entity}.");
                }

                if (role.Max < role.Min)
                    Log.Error($"ERT call {call.ID} role {role.Id} has max {role.Max} lower than min {role.Min}.");
            }

            var totalMax = call.Roles.Sum(GetRoleMaximumCount);
            var requiredMinimum = Math.Max(call.Requirements.MinRequiredSlots, call.Roles.Sum(GetRoleMinimumCount));
            var effectiveMax = call.Requirements.MaxSlots > 0
                ? Math.Min(totalMax, call.Requirements.MaxSlots)
                : totalMax;

            if (call.Requirements.MaxSlots < 0)
                Log.Error($"ERT call {call.ID} has negative maxSlots {call.Requirements.MaxSlots}.");

            if (requiredMinimum > effectiveMax)
            {
                Log.Error($"ERT call {call.ID} requires at least {requiredMinimum} roster slots, but only {effectiveMax} are available.");
            }
        }
    }

    private string FormatEntity(EntityUid? entity)
    {
        return entity is { Valid: true } uid && Exists(uid)
            ? ToPrettyString(uid)
            : "none";
    }

    private string GetShuttleDiagnostics(EntityUid? shuttle)
    {
        if (shuttle is not { Valid: true } shuttleUid || !Exists(shuttleUid))
            return "ShuttleState=missing";

        var xform = Transform(shuttleUid);
        var mapId = xform.MapID;
        var mapInitialized = _map.MapExists(mapId) && _map.IsInitialized(mapId);
        var mapPaused = _map.MapExists(mapId) && _map.IsPaused(mapId);
        var gridPaused = MetaData(shuttleUid).EntityPaused;
        var navComputers = CountNavigationComputers(shuttleUid);
        var actors = CountActorsOnShuttle(shuttleUid);

        return $"ShuttleState=map:{mapId}, mapInit:{mapInitialized}, mapPaused:{mapPaused}, " +
               $"gridPaused:{gridPaused}, navComputers:{navComputers}, actors:{actors}";
    }

    private string GetShuttleDiagnostics(RMCERTRequest request, EntityUid? shuttle)
    {
        var diagnostics = GetShuttleDiagnostics(shuttle);
        if (shuttle is not { Valid: true } shuttleUid || !Exists(shuttleUid))
            return diagnostics;

        var landingZones = 0;
        if (TryFindNavigationComputer(shuttleUid, out var computer))
            landingZones = CountLandingZoneCandidates(request, computer);

        return $"{diagnostics}, spawnMarker:{FormatEntity(request.ShuttleSpawnMarker)}, landingZones:{landingZones}";
    }

    private int CountNavigationComputers(EntityUid shuttle)
    {
        var count = 0;
        var query = EntityQueryEnumerator<DropshipNavigationComputerComponent, TransformComponent>();
        while (query.MoveNext(out _, out _, out var xform))
        {
            if (xform.GridUid == shuttle)
                count++;
        }

        return count;
    }

    private int CountLandingZoneCandidates(
        RMCERTRequest request,
        Entity<DropshipNavigationComputerComponent> computer)
    {
        var count = 0;
        var sourceMap = GetRequestSourceMap(request);

        var query = EntityQueryEnumerator<DropshipDestinationComponent>();
        while (query.MoveNext(out var uid, out var dropshipDestination))
        {
            if (dropshipDestination.Ship != null)
                continue;

            if (sourceMap != null &&
                Transform(uid).MapUid != sourceMap)
            {
                continue;
            }

            if (!_dropship.CanUseDestinationForShuttle(computer, uid, out _))
                continue;

            count++;
        }

        return count;
    }

    private void LogLandingZoneDiagnostics(
        RMCERTRequest request,
        Entity<DropshipNavigationComputerComponent> computer)
    {
        var sourceMap = GetRequestSourceMap(request);

        var computerXform = Transform(computer);
        var builder = new StringBuilder();
        builder.Append($"ERT request {request.Id} has no valid landing zone. ");
        builder.Append($"call:{request.SelectedCall?.Id ?? "none"}, ");
        builder.Append($"source:{FormatEntity(request.SourceEntity)}, ");
        builder.Append($"sourceMap:{FormatEntity(sourceMap)}, ");
        builder.Append($"computer:{ToPrettyString(computer.Owner)}, ");
        builder.Append($"computerMap:{FormatEntity(computerXform.MapUid)}, ");
        builder.Append($"shuttle:{FormatEntity(computerXform.GridUid)}, ");
        builder.Append($"class:{computer.Comp.ShuttleDockingClass}, ");
        builder.Append($"bounds:{FormatNullable(computer.Comp.DockingBounds)}, ");
        builder.Append($"allowedTags:[{string.Join(", ", computer.Comp.AllowedLandingTags)}], ");
        builder.Append($"deniedTags:[{string.Join(", ", computer.Comp.DeniedLandingTags)}]");

        var total = 0;
        var accepted = 0;
        var rejected = 0;
        var query = EntityQueryEnumerator<DropshipDestinationComponent>();
        while (query.MoveNext(out var uid, out var dropshipDestination))
        {
            total++;
            var reasons = new List<string>();
            var destinationXform = Transform(uid);
            if (dropshipDestination.Ship is { } occupiedBy)
                reasons.Add($"occupiedBy={FormatEntity(occupiedBy)}");

            if (sourceMap != null && destinationXform.MapUid != sourceMap)
                reasons.Add($"mapMismatch destinationMap={FormatEntity(destinationXform.MapUid)} sourceMap={FormatEntity(sourceMap)}");

            if (!_dropship.CanUseDestinationForShuttle(computer, uid, out var reason))
                reasons.Add($"dropshipRule={reason}");

            if (reasons.Count == 0)
            {
                accepted++;
                continue;
            }

            rejected++;
            var meta = MetaData(uid);
            var prototype = meta.EntityPrototype?.ID ?? "none";
            var landingPolicy = $"destination enabled:{dropshipDestination.Enabled} reserved:{dropshipDestination.Reserved} " +
                                $"classes:[{string.Join(", ", dropshipDestination.LandingClasses)}] " +
                                $"tags:[{string.Join(", ", dropshipDestination.LandingTags)}]";

            builder.AppendLine();
            builder.Append($" - rejected {ToPrettyString(uid)} proto:{prototype} map:{FormatEntity(destinationXform.MapUid)} ");
            builder.Append($"dockBounds:{FormatNullable(dropshipDestination.DockBounds)} ship:{FormatEntity(dropshipDestination.Ship)} ");
            builder.Append($"{landingPolicy} reason:{string.Join("; ", reasons)}");
        }

        builder.AppendLine();
        builder.Append($"Landing zone totals: total:{total}, accepted:{accepted}, rejected:{rejected}");
        Log.Warning(builder.ToString());
    }

    private static string FormatNullable(object? value)
    {
        return value?.ToString() ?? "null";
    }

    private static string FormatRoundTime(TimeSpan time)
    {
        return $"{(int)time.TotalMinutes:00}:{time.Seconds:00}";
    }
}
