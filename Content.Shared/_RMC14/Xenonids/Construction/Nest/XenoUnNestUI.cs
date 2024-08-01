using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Construction.Nest;

[Serializable, NetSerializable]
public enum XenoUnNestUI : byte
{
    Key
}
public sealed class XenoUnNestBUI() : BoundUserInterfaceMessage
{
    public readonly string Text = "Ok";
}
