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
public sealed class TacticalMapXenoWatchBlipMsg(NetEntity target) : BoundUserInterfaceMessage
{
    public readonly NetEntity Target = target;
}

[Serializable, NetSerializable]
public sealed class TacticalMapSetVisibleLayersMsg(List<string> layerIds) : BoundUserInterfaceMessage
{
    public readonly List<string> LayerIds = layerIds;
}
