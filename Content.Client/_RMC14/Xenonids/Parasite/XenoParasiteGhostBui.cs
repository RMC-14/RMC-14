using Content.Shared._RMC14.Xenonids.Egg;
using JetBrains.Annotations;

namespace Content.Client._RMC14.Xenonids.Parasite;

[UsedImplicitly]
public sealed class XenoParasiteGhostBui : BoundUserInterface
{
    [ViewVariables]
    private XenoParasiteGhostWindow? _window;

    public XenoParasiteGhostBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = new XenoParasiteGhostWindow();
        _window.OnClose += Close;
        _window.DenyButton.OnPressed += _ => _window.Close();
        _window.ConfirmButton.OnPressed += _ => SendPredictedMessage(new XenoParasiteGhostBuiMsg());

        _window.OpenCentered();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            _window?.Dispose();
    }
}
