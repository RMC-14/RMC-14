using Content.Server.Access.Systems;
using Content.Server.Medical.SuitSensors;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Roles;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared.Access.Components;
using Content.Shared.Medical.SuitSensor;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Content.Shared.Roles;
using Content.Shared.StatusIcon;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Medical.CrewMonitoring;

public sealed class RMCCrewMonitorDataSystem : EntitySystem
{
    [Dependency] private readonly GunIFFSystem _gunIFF = default!;
    [Dependency] private readonly IdCardSystem _idCard = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;

    public void CollectSensors(
        IReadOnlySet<ProtoId<NpcFactionPrototype>> npcFactions,
        IReadOnlySet<EntProtoId<IFFFactionComponent>> iffFactions,
        SuitSensorMode minimumMode,
        Dictionary<EntityUid, Entity<SuitSensorComponent>> sensors,
        MapId? requiredMap = null)
    {
        sensors.Clear();

        var query = EntityQueryEnumerator<SuitSensorComponent>();
        while (query.MoveNext(out var sensorId, out var sensor))
        {
            if (sensor.Mode < minimumMode ||
                sensor.User is not { } user ||
                !IsTrackedSensor((sensorId, sensor), user, npcFactions, iffFactions, requiredMap))
            {
                continue;
            }

            if (sensors.TryGetValue(user, out var existing) && existing.Comp.Mode >= sensor.Mode)
                continue;

            sensors[user] = (sensorId, sensor);
        }
    }

    public bool IsTrackedSensor(
        Entity<SuitSensorComponent> sensor,
        EntityUid user,
        IReadOnlySet<ProtoId<NpcFactionPrototype>> npcFactions,
        IReadOnlySet<EntProtoId<IFFFactionComponent>> iffFactions,
        MapId? requiredMap = null)
    {
        if (sensor.Comp.User != user ||
            sensor.Comp.Mode == SuitSensorMode.SensorOff ||
            TerminatingOrDeleted(user) ||
            (!HasComp<ActorComponent>(user) && !HasComp<OriginalRoleComponent>(user)) ||
            !TryComp(user, out TransformComponent? xform) ||
            !IsValidMap(xform.MapID) ||
            requiredMap != null && xform.MapID != requiredMap ||
            !IsTrackedFaction(user, npcFactions, iffFactions))
        {
            return false;
        }

        return true;
    }

    public bool IsValidMap(MapId mapId)
    {
        return _map.TryGetMap(mapId, out var map) &&
               !_map.IsPaused(map.Value) &&
               (HasComp<AlmayerComponent>(map) || HasComp<RMCPlanetComponent>(map));
    }

    public RMCCrewMonitorIdentity GetIdentity(EntityUid user)
    {
        var name = Loc.GetString("suit-sensor-component-unknown-name");
        var jobTitle = Loc.GetString("suit-sensor-component-unknown-job");
        ProtoId<JobIconPrototype> jobIcon = "JobIconNoId";
        var departments = new List<ProtoId<DepartmentPrototype>>();

        if (_idCard.TryFindIdCard(user, out var card))
        {
            if (!string.IsNullOrWhiteSpace(card.Comp.FullName))
                name = card.Comp.FullName;
            if (!string.IsNullOrWhiteSpace(card.Comp.LocalizedJobTitle))
                jobTitle = card.Comp.LocalizedJobTitle;
            jobIcon = card.Comp.JobIcon;
            departments.AddRange(card.Comp.JobDepartments);
        }

        ProtoId<JobPrototype>? job = null;
        if (TryComp(user, out OriginalRoleComponent? originalRole))
            job = originalRole.Job;

        return new RMCCrewMonitorIdentity(name, jobTitle, jobIcon, departments, job);
    }

    private bool IsTrackedFaction(
        EntityUid user,
        IReadOnlySet<ProtoId<NpcFactionPrototype>> npcFactions,
        IReadOnlySet<EntProtoId<IFFFactionComponent>> iffFactions)
    {
        if (_npcFaction.IsMemberOfAny((user, null), npcFactions))
            return true;

        foreach (var faction in iffFactions)
        {
            if (_gunIFF.IsInFaction(user, faction))
                return true;
        }

        return false;
    }
}

public readonly record struct RMCCrewMonitorIdentity(
    string Name,
    string JobTitle,
    ProtoId<JobIconPrototype> JobIcon,
    List<ProtoId<DepartmentPrototype>> Departments,
    ProtoId<JobPrototype>? Job);
