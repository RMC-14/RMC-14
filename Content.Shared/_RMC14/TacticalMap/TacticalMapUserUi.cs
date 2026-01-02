using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.TacticalMap;

[Serializable, NetSerializable]
public enum TacticalMapUserUi
{
    Key,
}

[Serializable, NetSerializable]
public sealed class TacticalMapBuiState(string mapName) : BoundUserInterfaceState
{
    public readonly string MapName = mapName;
}
