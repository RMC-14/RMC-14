using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.TacticalMap;

[Serializable, NetSerializable]
public sealed class XenoRemoteStructureRemovalBuiMsg(NetEntity target) : BoundUserInterfaceMessage
{
    public readonly NetEntity Target = target;
}

[Serializable, NetSerializable]
public sealed record XenoRemoteStructureRemovalConfirmEvent(NetEntity Actor, NetEntity Target, bool Cancel);
