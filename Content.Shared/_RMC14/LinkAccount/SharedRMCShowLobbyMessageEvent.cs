using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.LinkAccount;

[Serializable, NetSerializable]
public sealed class SharedRMCShowLobbyMessageEvent(string text) : EntityEventArgs
{
    public readonly string Text = text;
}
