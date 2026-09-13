using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Medical.CryoCell;

[Serializable, NetSerializable]
public sealed class CryoCellEjectConfirmationRequestEvent : EntityEventArgs
{
    public NetEntity CryoCell;
}

[Serializable, NetSerializable]
public sealed class CryoCellEjectConfirmationMessage(bool accepted) : EuiMessageBase
{
    public readonly bool Accepted = accepted;
}
