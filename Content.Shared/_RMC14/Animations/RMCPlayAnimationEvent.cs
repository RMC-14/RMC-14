using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Animations;

[Serializable, NetSerializable]
public sealed class RMCPlayAnimationEvent(NetEntity entity, RMCAnimationId animation) : EntityEventArgs
{
    public NetEntity Entity = entity;
    public RMCAnimationId Animation = animation;
}
