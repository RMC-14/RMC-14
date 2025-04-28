using Content.Shared._RMC14.Xenonids.Word;
using JetBrains.Annotations;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client._RMC14.Xenonids.Word;

[UsedImplicitly]
public sealed class XenoWordQueenBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private XenoWordQueenWindow? _window;

    protected override void Open()
    {
        base.Open();
        _window = new XenoWordQueenWindow();
        _window.OnClose += Close;

        _window.SendButton.OnPressed += Send;

        _window.OpenCentered();
    }

    private void Send(ButtonEventArgs args)
    {
        if (_window == null)
            return;

        var text = Rope.Collapse(_window.Text.TextRope);
        if (string.IsNullOrWhiteSpace(text))
            return;

        var msg = new XenoWordQueenBuiMsg(text);
        SendPredictedMessage(msg);
        _window.Close();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _window?.Dispose();
    }
}
