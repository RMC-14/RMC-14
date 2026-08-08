using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Dialog;

[Serializable, NetSerializable]
public struct DialogOption(string text, object? ev = null, SpriteSpecifier? icon = null)
{
    public string Text = text;

    // Event payloads are only used on the server once the client responds.
    // They should never be part of the replicated UI state.
    [NonSerialized]
    public object? Event = ev;

    public SpriteSpecifier? Icon = icon;
}
