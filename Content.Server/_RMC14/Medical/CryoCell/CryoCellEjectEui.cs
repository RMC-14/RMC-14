using Content.Server.EUI;
using Content.Shared._RMC14.Medical.CryoCell;
using Content.Shared.Eui;
using Robust.Shared.Player;

namespace Content.Server._RMC14.Medical.CryoCell;

public sealed class CryoCellEjectEui(CryoCellSystem cryoCell, EntityUid cell, ICommonSession session) : BaseEui
{
    public override void HandleMessage(EuiMessageBase msg)
    {
        if (msg is not CryoCellEjectConfirmationMessage { Accepted: true } ||
            session.AttachedEntity is not { } user)
        {
            Close();
            return;
        }

        cryoCell.TryEjectFromInside(cell, user);

        Close();
    }
}
