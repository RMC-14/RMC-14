using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Dialog;

[Serializable, NetSerializable]
public struct DialogOption(string text, object? ev = null)
{
    public string Text = text;
    public object? Event = ev;
}
