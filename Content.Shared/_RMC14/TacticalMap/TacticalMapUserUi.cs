using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.TacticalMap;

[Serializable, NetSerializable]
public enum TacticalMapUserUi
{
    Key,
}

[Serializable, NetSerializable]
public readonly record struct TacticalMapMapInfo(NetEntity Map, string MapId, string DisplayName);

[Serializable, NetSerializable]
public sealed class TacticalMapBuiState(NetEntity activeMap, List<TacticalMapMapInfo> maps) : BoundUserInterfaceState
{
    public readonly NetEntity ActiveMap = activeMap;
    public readonly List<TacticalMapMapInfo> Maps = maps;
}
