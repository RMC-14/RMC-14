using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Atmos;

[Serializable, NetSerializable]
public sealed class RMCStopDropRollVisualsNetworkEvent(NetEntity user) : EntityEventArgs
{
    public readonly NetEntity User = user;
}
