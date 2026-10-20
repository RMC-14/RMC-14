using Content.Shared._RMC14.Chemistry.Centrifuge;
using Content.Shared._RMC14.UserInterface;
using Content.Shared.Containers.ItemSlots;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._RMC14.Chemistry.Centrifuge;

[UsedImplicitly]
public sealed class RMCCentrifugeBui : BoundUserInterface, IRefreshableBui
{
    private readonly ItemSlotsSystem _itemSlots;

    private RMCCentrifugeWindow? _window;

    public RMCCentrifugeBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _itemSlots = EntMan.System<ItemSlotsSystem>();
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<RMCCentrifugeWindow>();

        _window.ModeButton.OnPressed += _ => SendPredictedMessage(new RMCCentrifugeToggleModeBuiMsg());
        _window.SourceButton.OnPressed += _ => SendPredictedMessage(new RMCCentrifugeToggleSourceBuiMsg());
        _window.ConnectButton.OnPressed += _ => SendPredictedMessage(new RMCCentrifugeAttemptConnectionBuiMsg());
        _window.EjectInputButton.OnPressed += _ => SendPredictedMessage(new RMCCentrifugeEjectInputBuiMsg());
        _window.EjectOutputButton.OnPressed += _ => SendPredictedMessage(new RMCCentrifugeEjectOutputBuiMsg());
        _window.LabelEdit.OnTextEntered += args => SendPredictedMessage(new RMCCentrifugeSetLabelBuiMsg(args.Text));

        Refresh();
    }

    public void Refresh()
    {
        if (_window is not { IsOpen: true })
            return;

        if (!EntMan.TryGetComponent(Owner, out RMCCentrifugeComponent? comp))
            return;

        _window.SetMode(comp.Mode == CentrifugeMode.Split ? "Mode: split" : "Mode: distribute");
        _window.SetSource(comp.InputSource == CentrifugeInputSource.Turing
            ? "Input: Turing Dispenser"
            : "Input: Container");

        var connected = comp.TuringDispenser != null;
        _window.SourceButton.Disabled = !connected;
        _window.ConnectButton.Visible = !connected;
        _window.SetConnection(connected);
        _window.SetLabel(comp.Label);

        var hasInput = _itemSlots.GetItemOrNull(Owner, comp.InputSlotId) != null;
        _window.InputStatusLabel.Text = hasInput ? "Loaded" : "Empty";
        _window.EjectInputButton.Disabled = !hasInput || comp.Spinning;

        var hasOutput = _itemSlots.GetItemOrNull(Owner, comp.OutputBoxSlotId) != null;
        _window.OutputStatusLabel.Text = hasOutput ? "Loaded" : "Empty";
        _window.EjectOutputButton.Disabled = !hasOutput || comp.Spinning;
    }
}
