using Content.Shared.Mobs;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Overwatch;

[Serializable, NetSerializable]
public enum OverwatchLocation
{
    Min = 0,
    Planet = 0,
    Ship = 1,
    Max = 1,
}

[Serializable, NetSerializable]
public enum OverwatchConsoleUI
{
    Key,
}

[Serializable, NetSerializable]
public sealed class OverwatchConsoleBuiState(List<OverwatchSquad> squads, Dictionary<NetEntity, List<OverwatchMarine>> marines) : BoundUserInterfaceState
{
    public readonly List<OverwatchSquad> Squads = squads;
    public readonly Dictionary<NetEntity, List<OverwatchMarine>> Marines = marines;
}

[Serializable, NetSerializable]
public sealed class OverwatchConsoleSelectSquadBuiMsg(NetEntity squad) : BoundUserInterfaceMessage
{
    public readonly NetEntity Squad = squad;
}

[Serializable, NetSerializable]
public sealed class OverwatchViewTacticalMapBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class OverwatchConsoleTakeOperatorBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class OverwatchConsoleStopOverwatchBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class OverwatchConsoleSetLocationBuiMsg(OverwatchLocation? location) : BoundUserInterfaceMessage
{
    public readonly OverwatchLocation? Location = location;
}

[Serializable, NetSerializable]
public sealed class OverwatchConsoleShowDeadBuiMsg(bool show) : BoundUserInterfaceMessage
{
    public readonly bool Show = show;
}

[Serializable, NetSerializable]
public sealed class OverwatchConsoleShowHiddenBuiMsg(bool show) : BoundUserInterfaceMessage
{
    public readonly bool Show = show;
}

[Serializable, NetSerializable]
public sealed class OverwatchConsoleWatchBuiMsg(NetEntity target) : BoundUserInterfaceMessage
{
    public readonly NetEntity Target = target;
}

[Serializable, NetSerializable]
public sealed class OverwatchConsoleHideBuiMsg(NetEntity target, bool hide) : BoundUserInterfaceMessage
{
    public readonly NetEntity Target = target;
    public readonly bool Hide = hide;
}

[Serializable, NetSerializable]
public readonly record struct OverwatchSquad(NetEntity Id, string Name, Color Color);

[Serializable, NetSerializable]
public readonly record struct OverwatchMarine(
    NetEntity Marine,
    NetEntity Camera,
    string Name,
    MobState State,
    bool SSD,
    ProtoId<JobPrototype>? Role,
    bool Deployed,
    OverwatchLocation Location
);
