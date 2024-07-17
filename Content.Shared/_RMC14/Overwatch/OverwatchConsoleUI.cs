using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Overwatch;

[Serializable, NetSerializable]
public enum OverwatchConsoleUI
{
    Key,
}

[Serializable, NetSerializable]
public sealed class OverwatchConsoleBuiState(List<OverwatchMarine> marines) : BoundUserInterfaceState
{
    public readonly List<OverwatchMarine> Marines = marines;
}

[Serializable, NetSerializable]
public sealed class OverwatchConsoleWatchBuiMsg(NetEntity target) : BoundUserInterfaceMessage
{
    public readonly NetEntity Target = target;
}

[Serializable, NetSerializable]
public readonly record struct OverwatchMarine(NetEntity Id, string SquadName, string Name);
