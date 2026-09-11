using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Medical.CryoCell;

[Serializable, NetSerializable]
public sealed class CryoCellEjectConfirmationRequestEvent : EntityEventArgs
{
    public NetEntity CryoCell;
}

public sealed class CryoCellEjectConfirmationMessage : EuiMessageBase
{
    public bool Accepted { get; }

    public CryoCellEjectConfirmationMessage(bool accepted)
    {
        Accepted = accepted;
    }
}
