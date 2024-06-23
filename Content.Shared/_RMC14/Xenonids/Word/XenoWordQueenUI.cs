using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Word;

[Serializable, NetSerializable]
public enum XenoWordQueenUI : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class XenoWordQueenBuiMsg(string text) : BoundUserInterfaceMessage
{
    public readonly string Text = text;
}
