using System.Linq;
using System.Numerics;
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
        _calls.Clear();
        foreach (var prototype in _prototypes.EnumeratePrototypes<EntityPrototype>())
        {
            if (prototype.Abstract || !prototype.TryGetComponent<RMCERTCallComponent>(out var component, _componentFactory))
                continue;

            var call = new RMCERTCall(prototype, component);
            _calls.Add(call.Id, call);
        }

        foreach (var call in _calls.Values)
        {
            if (call.Comp.Roles.Count == 0 &&
                !IsCargoOnlyShuttle(call))
            {
                Log.Error($"ERT call {call.Id} has no roles configured.");
            }

            if (!TryGetShuttleMapPath(call, out _, out var shuttleError))
                Log.Error($"ERT call {call.Id} has invalid shuttle configuration: {shuttleError}");

            if (call.Comp.ShuttleCargo.Count > 0 || call.Comp.ShuttleCargoCount > 0)
            {
                if (call.Comp.ShuttleCargo.Count == 0)
                    Log.Error($"ERT call {call.Id} has shuttleCargoCount but no shuttle cargo prototypes.");

                foreach (var cargo in call.Comp.ShuttleCargo)
                {
                    if (!_prototypes.HasIndex<EntityPrototype>(cargo))
                        Log.Error($"ERT call {call.Id} references missing shuttle cargo entity {cargo}.");
                }
            }

            if (call.Comp.ShuttleSpawnMarker is { } spawnMarker)
            {
                if (!_prototypes.TryIndex(spawnMarker, out var spawnMarkerPrototype))
                {
                    Log.Error($"ERT call {call.Id} references missing shuttle spawn marker {spawnMarker.Id}.");
                }
                else if (!spawnMarkerPrototype.HasComponent<RMCERTShuttleStartPadComponent>(_componentFactory) &&
                         !spawnMarkerPrototype.HasComponent<GridSpawnerComponent>(_componentFactory))
                {
                    Log.Error($"ERT call {call.Id} shuttle spawn marker {spawnMarker.Id} is missing RMCERTShuttleStartPadComponent.");
                }
            }

            if (string.IsNullOrWhiteSpace(call.Organization) &&
                call.Comp.NpcFactions.Count == 0 &&
                call.Comp.IffFaction == null)
            {
                Log.Error($"ERT call {call.Id} is missing organization, NPC factions, and IFF faction metadata.");
            }

            foreach (var role in call.Comp.Roles)
            {
                if (role.GhostRoleEntityPool.Count == 0)
                    Log.Error($"ERT call {call.Id} role {role.Id} has no ghost role entity pool entries.");

                foreach (var entry in role.GhostRoleEntityPool)
                {
                    if (entry.Weight <= 0)
                        Log.Error($"ERT call {call.Id} role {role.Id} has non-positive ghost role entity pool weight {entry.Weight} for {entry.Entity}.");

                    if (!_prototypes.HasIndex<EntityPrototype>(entry.Entity))
                        Log.Error($"ERT call {call.Id} role {role.Id} references missing pooled ghost role entity {entry.Entity}.");
                }

                if (role.Max < role.Min)
                    Log.Error($"ERT call {call.Id} role {role.Id} has max {role.Max} lower than min {role.Min}.");
            }

            var totalMax = call.Comp.Roles.Sum(GetRoleMaximumCount);
            var requiredMinimum = Math.Max(call.Comp.Requirements.MinRequiredSlots, call.Comp.Roles.Sum(GetRoleMinimumCount));
            var effectiveMax = call.Comp.Requirements.MaxSlots > 0
                ? Math.Min(totalMax, call.Comp.Requirements.MaxSlots)
                : totalMax;

            if (call.Comp.Requirements.MaxSlots < 0)
                Log.Error($"ERT call {call.Id} has negative maxSlots {call.Comp.Requirements.MaxSlots}.");

            if (requiredMinimum > effectiveMax)
            {
                Log.Error($"ERT call {call.Id} requires at least {requiredMinimum} roster slots, but only {effectiveMax} are available.");
            }
        }
    }

    private string FormatEntity(EntityUid? entity)
    {
        return entity is { Valid: true } uid && Exists(uid)
            ? ToPrettyString(uid)
            : "none";
    }

    private static string FormatRoundTime(TimeSpan time)
    {
        return $"{(int)time.TotalMinutes:00}:{time.Seconds:00}";
    }
}
