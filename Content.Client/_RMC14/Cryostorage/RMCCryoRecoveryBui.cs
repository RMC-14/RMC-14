using Content.Shared._RMC14.Cryostorage;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._RMC14.Cryostorage;

/// <summary>
/// Thin client BUI for the requisitions cryo recovery console.
/// All item ownership and access checks live on the server.
/// </summary>
[UsedImplicitly]
public sealed class RMCCryoRecoveryBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private RMCCryoRecoveryWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<RMCCryoRecoveryWindow>();
        _window.Title = Loc.GetString("rmc-cryo-recovery-window-title");
        _window.SetBui(this);

        if (State is RMCCryoRecoveryBuiState state)
            _window.UpdateState(state);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is RMCCryoRecoveryBuiState recoveryState)
            _window?.UpdateState(recoveryState);
    }

    public void RecoverItem(NetEntity player, NetEntity item)
    {
        SendMessage(new RMCCryoRecoveryRecoverItemBuiMsg(player, item));
    }

    public void RecoverAll(NetEntity player)
    {
        SendMessage(new RMCCryoRecoveryRecoverAllBuiMsg(player));
    }
}
