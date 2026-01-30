using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Construction.Events;

[Serializable, NetSerializable]
public sealed class XenoConstructionAnimationStartEvent(NetEntity effect, NetEntity xeno, TimeSpan buildTime) : EntityEventArgs
{
    public readonly NetEntity Effect = effect;
    public readonly NetEntity Xeno = xeno;
    public readonly TimeSpan BuildTime = buildTime;
}
