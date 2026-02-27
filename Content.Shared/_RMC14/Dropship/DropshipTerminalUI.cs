using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Dropship;

[Serializable, NetSerializable]
public enum DropshipTerminalUiKey
{
    Key,
}

[Serializable, NetSerializable]
public sealed class DropshipTerminalBuiState(string name, List<DropshipEntry> dropships) : BoundUserInterfaceState
{
    public readonly string Name = name;
    public readonly List<DropshipEntry> Dropships = dropships;
}

[Serializable, NetSerializable]
public readonly record struct DropshipEntry(NetEntity Id, string Name);

[Serializable, NetSerializable]
public sealed class DropshipTerminalSummonDropshipMsg(NetEntity id) : BoundUserInterfaceMessage
{
    public readonly NetEntity Id = id;
}
