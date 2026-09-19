using System.Numerics;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared.Mobs;
using Content.Shared.NPC.Prototypes;
using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Medical.CrewMonitoring;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
[Access(typeof(SharedRMCPortableCrewMonitorSystem))]
public sealed partial class RMCPortableCrewMonitorComponent : Component
{
    [DataField]
    public HashSet<ProtoId<NpcFactionPrototype>> NpcFactions = new();

    [DataField]
    public HashSet<EntProtoId<IFFFactionComponent>> IffFactions = new();

    [DataField, AutoNetworkedField]
    public float RadarRange = 24;

    [DataField]
    public TimeSpan ScanDuration = TimeSpan.FromSeconds(2);

    [DataField]
    public TimeSpan TrackEvery = TimeSpan.FromSeconds(0.5);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan ScanEndsAt;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextTrackAt;

    [DataField, AutoNetworkedField]
    public bool Scanning;

    [DataField, AutoNetworkedField]
    public bool HasScanned;

    [DataField, AutoNetworkedField]
    public List<RMCPortableCrewMonitorEntry> Signals = new();

    [DataField, AutoNetworkedField]
    public NetEntity? Selected;

    public readonly Dictionary<EntityUid, EntityUid> Sensors = new();

    public EntityUid? SelectedTarget;

    public EntityUid? SelectedSensor;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedRMCPortableCrewMonitorSystem))]
public sealed partial class RMCPortableCrewMonitorTrackingComponent : Component
{
    [DataField, AutoNetworkedField]
    public Vector2? Offset;

    [DataField, AutoNetworkedField]
    public bool DirectionOnly;
}

[Serializable, NetSerializable]
public readonly record struct RMCPortableCrewMonitorEntry(
    NetEntity Id,
    string Name,
    string JobTitle,
    ProtoId<JobIconPrototype> JobIcon,
    MobState State);

[Serializable, NetSerializable]
public enum RMCPortableCrewMonitorUIKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class RMCPortableCrewMonitorScanBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class RMCPortableCrewMonitorSelectBuiMsg(NetEntity target) : BoundUserInterfaceMessage
{
    public NetEntity Target = target;
}
