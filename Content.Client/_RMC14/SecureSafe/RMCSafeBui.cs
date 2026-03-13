using Content.Shared._RMC14.SecureSafe;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.SecureSafe;

public sealed class RMCSafeBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private RMCSafeWindow? _window;

    public RMCSafeBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = new RMCSafeWindow();
        
        _window.OnClose += Close;
        
        _window.Dial1Plus.OnPressed += _ =>
        {
            if (_window.CheckCooldown())
                SendMessage(new RMCSafeChangeDialMessage(1, 5));
        };
        _window.Dial1Minus.OnPressed += _ =>
        {
            if (_window.CheckCooldown())
                SendMessage(new RMCSafeChangeDialMessage(1, -5));
        };
        
        _window.Dial2Plus.OnPressed += _ =>
        {
            if (_window.CheckCooldown())
                SendMessage(new RMCSafeChangeDialMessage(2, 5));
        };
        _window.Dial2Minus.OnPressed += _ =>
        {
            if (_window.CheckCooldown())
                SendMessage(new RMCSafeChangeDialMessage(2, -5));
        };
        
        _window.OpenButton.OnPressed += _ => SendMessage(new RMCSafeTryOpenMessage());

        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not RMCSafeBuiState safeState)
            return;

        _window?.UpdateState(safeState.Dial1, safeState.Dial2);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _window?.Dispose();
        }
    }
}
