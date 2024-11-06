using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Dialog;

[Serializable, NetSerializable]
public struct DialogChoice(string text, object? ev)
{
    public string Text = text;
    public object? Event = ev;

    public DialogChoice(string text) : this(text, null)
    {
    }
}
