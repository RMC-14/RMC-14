using JetBrains.Annotations;
using Content.Shared._RMC14.Xenonids.Egg;

namespace Content.Client._RMC14.Xenonids.Egg;

[UsedImplicitly]
public sealed class XenoEggGhostBui : BoundUserInterface
{
    [ViewVariables]
    private XenoEggGhostWindow? _window;

    public XenoEggGhostBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        _window = new XenoEggGhostWindow();
        _window.OnClose += Close;
        _window.DenyButton.OnPressed += _ => _window.Close();
        _window.ConfirmButton.OnPressed += _ => SendPredictedMessage(new XenoEggGhostBuiMsg());

        _window.OpenCentered();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            _window?.Dispose();
    }
}