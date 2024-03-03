using Robust.Shared.Serialization;

namespace Content.Shared._CM14.Xenos.Word;

[Serializable, NetSerializable]
public enum XenoWordQueenUI : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class XenoWordQueenBuiMessage : BoundUserInterfaceMessage
{
    public readonly string Text;

    public XenoWordQueenBuiMessage(string text)
    {
        Text = text;
    }
}
