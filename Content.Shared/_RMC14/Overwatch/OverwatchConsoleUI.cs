using System.Numerics;
using Content.Shared._RMC14.Marines.Roles.Ranks;
using Content.Shared.Mobs;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

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
public sealed class OverwatchConsoleBuiState(
    List<OverwatchSquad> squads,
    Dictionary<NetEntity, List<OverwatchMarine>> marines) : BoundUserInterfaceState
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
public sealed class OverwatchConsoleTransferMarineBuiMsg : BoundUserInterfaceMessage;

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
public sealed class OverwatchConsolePromoteLeaderBuiMsg(NetEntity target, SpriteSpecifier.Rsi icon) : BoundUserInterfaceMessage
{
    public readonly NetEntity Target = target;
    public readonly SpriteSpecifier.Rsi Icon = icon;
}

[Serializable, NetSerializable]
public sealed class OverwatchConsoleSupplyDropLongitudeBuiMsg(int longitude) : BoundUserInterfaceMessage
{
    public readonly int Longitude = longitude;
}

[Serializable, NetSerializable]
public sealed class OverwatchConsoleSupplyDropLatitudeBuiMsg(int latitude) : BoundUserInterfaceMessage
{
    public readonly int Latitude = latitude;
}

[Serializable, NetSerializable]
public sealed class OverwatchConsoleSupplyDropLaunchBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class OverwatchConsoleSupplyDropSaveBuiMsg(int longitude, int latitude) : BoundUserInterfaceMessage
{
    public readonly int Longitude = longitude;
    public readonly int Latitude = latitude;
}

[Serializable, NetSerializable]
public sealed class OverwatchConsoleLocationCommentBuiMsg(int index, string comment) : BoundUserInterfaceMessage
{
    public readonly int Index = index;
    public readonly string Comment = comment;
}

[Serializable, NetSerializable]
public sealed class OverwatchConsoleOrbitalLongitudeBuiMsg(int longitude) : BoundUserInterfaceMessage
{
    public readonly int Longitude = longitude;
}

[Serializable, NetSerializable]
public sealed class OverwatchConsoleOrbitalLatitudeBuiMsg(int latitude) : BoundUserInterfaceMessage
{
    public readonly int Latitude = latitude;
}

[Serializable, NetSerializable]
public sealed class OverwatchConsoleOrbitalLaunchBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class OverwatchConsoleOrbitalSaveBuiMsg(int longitude, int latitude) : BoundUserInterfaceMessage
{
    public readonly int Longitude = longitude;
    public readonly int Latitude = latitude;
}

[Serializable, NetSerializable]
public sealed class OverwatchConsoleOrbitalCommentBuiMsg(int index, string comment) : BoundUserInterfaceMessage
{
    public readonly int Index = index;
    public readonly string Comment = comment;
}

[Serializable, NetSerializable]
public sealed class OverwatchConsoleSendMessageBuiMsg(string message) : BoundUserInterfaceMessage
{
    public readonly string Message = message;
}

[Serializable, NetSerializable]
public record struct OverwatchSquad(NetEntity Id, string Name, Color Color, NetEntity? Leader, bool CanSupplyDrop, SpriteSpecifier.Rsi LeaderIcon);

[Serializable, NetSerializable]
public readonly record struct OverwatchMarine(
    NetEntity Id,
    NetEntity Camera,
    string Name,
    MobState State,
    bool SSD,
    ProtoId<JobPrototype>? Role,
    bool Deployed,
    OverwatchLocation Location,
    string AreaName,
    Vector2? LeaderDistance,
    ProtoId<RankPrototype>? Rank
);
