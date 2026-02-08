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

        if (State is SleeperBuiState state)
            UpdateState(state);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is SleeperBuiState sleeperState)
            UpdateState(sleeperState);
    }

    private void UpdateState(SleeperBuiState state)
    {
        if (_window == null)
        {
            _window = this.CreateWindow<SleeperConsoleWindow>();
            _window.Title = Loc.GetString("rmc-sleeper-window-title");
            _window.SetBui(this);
        }

        _window.UpdateState(state);
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
