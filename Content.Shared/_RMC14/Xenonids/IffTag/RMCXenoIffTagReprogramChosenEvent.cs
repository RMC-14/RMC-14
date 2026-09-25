using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.IffTag;

[Serializable, NetSerializable]
public enum RMCXenoIffTagReprogramOption : byte
{
    Overwrite,
    Add,
    Remove,
}

[ByRefEvent]
[Serializable, NetSerializable]
public readonly record struct RMCXenoIffTagReprogramChosenEvent(RMCXenoIffTagReprogramOption Option);
