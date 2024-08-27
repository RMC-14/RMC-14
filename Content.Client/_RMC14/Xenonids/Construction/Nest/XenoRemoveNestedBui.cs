using JetBrains.Annotations;
using Content.Shared._RMC14.Xenonids.Construction.Nest;

namespace Content.Client._RMC14.Xenonids.Construction.Nest;

[UsedImplicitly]
public sealed class XenoRemoveNestedBui : BoundUserInterface
{

    private int _nestableTarget;
    [ViewVariables]
    private XenoRemoveNestedWindow? _window;

    public XenoRemoveNestedBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        _window = new XenoRemoveNestedWindow();
        _window.OnClose += Close;

        _window.ConfirmButton.OnPressed += _ => RemoveNested();

        _window.OpenCentered();
    }

    private void RemoveNested()
    {
        if (_window == null)
            return;
        var msg = new XenoRemoveNestedBuiMsg();
        SendPredictedMessage(msg);
        //_window.Close();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _window?.Dispose();
    }
}
