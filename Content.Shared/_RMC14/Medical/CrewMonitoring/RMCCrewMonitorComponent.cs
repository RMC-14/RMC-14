using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared.Medical.SuitSensor;
using Content.Shared.Mobs;
using Content.Shared.NPC.Prototypes;
using Content.Shared.Roles;
using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Medical.CrewMonitoring;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedRMCCrewMonitorSystem))]
public sealed partial class RMCCrewMonitorComponent : Component
{
    [DataField]
    public HashSet<ProtoId<NpcFactionPrototype>> NpcFactions = new();

    [DataField]
    public HashSet<EntProtoId<IFFFactionComponent>> IffFactions = new();

    [DataField, AutoNetworkedField]
    public List<RMCCrewMonitorEntry> Entries = new();
}

[Serializable, NetSerializable]
public readonly record struct RMCCrewMonitorEntry(
    NetEntity Id,
    string Name,
    string JobTitle,
    ProtoId<JobPrototype>? Job,
    ProtoId<JobIconPrototype> JobIcon,
    List<ProtoId<DepartmentPrototype>> Departments,
    string? Squad,
    Color? SquadColor,
    SuitSensorMode SensorMode,
    MobState State,
    float? Brute,
    float? Burn,
    float? Toxin,
    float? Oxygen,
    RMCCrewMonitorLocation? Location,
    string? AreaName);

[Serializable, NetSerializable]
public enum RMCCrewMonitorLocation : byte
{
    Ship,
    Planet,
}

[Serializable, NetSerializable]
public enum RMCCrewMonitorUIKey : byte
{
    Key,
}

public enum RMCCrewMonitorVisuals : byte
{
    Broken,
}

public enum RMCCrewMonitorVisualLayers : byte
{
    Unpowered,
    Broken,
}

[Serializable, NetSerializable]
public sealed class RMCCrewMonitorRefreshBuiMsg : BoundUserInterfaceMessage;
