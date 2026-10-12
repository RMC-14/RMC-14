using Content.Client.Eui;
using Content.Shared._RMC14.Marines.Icons;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Client._RMC14.Marines.Icons;

[UsedImplicitly]
public sealed class JobIconPickerEui : BaseEui
{
    private readonly JobIconPickerWindow _window;

    public JobIconPickerEui()
    {
        _window = new JobIconPickerWindow();
        _window.OnClose += () => SendMessage(new CloseEuiMessage());
        _window.OnIconSelected += (rsi, state) => SendMessage(new JobIconPickerSelectMessage(rsi, state));
        _window.OnClear += () => SendMessage(new JobIconPickerClearMessage());
    }

    public override void Opened()
    {
        _window.Populate();
        _window.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();
        _window.Close();
    }
}
