using Content.Shared._RMC14.Medical.MedicalPods;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._RMC14.Medical.MedicalPods;

[UsedImplicitly]
public sealed class SleeperConsoleBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private SleeperConsoleWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<SleeperConsoleWindow>();
        _window.Title = Loc.GetString("rmc-sleeper-window-title");
        _window.SetBui(this);
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        base.ReceiveMessage(message);

        if (message is SleeperBuiState sleeperState)
            _window?.UpdateState(sleeperState);
    }

    public void InjectChemical(string chemicalId, int amount)
    {
        SendMessage(new SleeperInjectChemicalBuiMsg(chemicalId, amount));
    }

    public void ToggleFilter()
    {
        SendMessage(new SleeperToggleFilterBuiMsg());
    }

    public void Eject()
    {
        SendMessage(new SleeperEjectBuiMsg());
    }

    public void SetAutoEjectDead(bool enabled)
    {
        SendMessage(new SleeperAutoEjectDeadBuiMsg(enabled));
    }
}
