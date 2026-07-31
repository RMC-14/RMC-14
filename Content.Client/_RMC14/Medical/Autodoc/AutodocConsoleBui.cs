using Content.Shared._RMC14.Medical.Autodoc;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._RMC14.Medical.Autodoc;

[UsedImplicitly]
public sealed class AutodocConsoleBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private AutodocConsoleWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<AutodocConsoleWindow>();
        _window.Title = Loc.GetString("rmc-autodoc-window-title");
        _window.SetBui(this);

        if (State is AutodocBuiState state)
            _window.UpdateState(state);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is AutodocBuiState autodocState)
            _window?.UpdateState(autodocState);
    }

    public void ToggleBrute()
    {
        SendMessage(new AutodocToggleBruteBuiMsg());
    }

    public void ToggleBurn()
    {
        SendMessage(new AutodocToggleBurnBuiMsg());
    }

    public void ToggleToxin()
    {
        SendMessage(new AutodocToggleToxinBuiMsg());
    }

    public void ToggleBlood()
    {
        SendMessage(new AutodocToggleBloodBuiMsg());
    }

    public void ToggleDialysis()
    {
        SendMessage(new AutodocToggleDialysisBuiMsg());
    }

    public void ToggleLarva()
    {
        SendMessage(new AutodocToggleLarvaBuiMsg());
    }

    public void ToggleCloseIncisions()
    {
        SendMessage(new AutodocToggleCloseIncisionsBuiMsg());
    }

    public void ToggleRemoveShrapnel()
    {
        SendMessage(new AutodocToggleRemoveShrapnelBuiMsg());
    }

    public void ToggleInternalBleeding()
    {
        SendMessage(new AutodocToggleInternalBleedingBuiMsg());
    }

    public void ToggleBrokenBone()
    {
        SendMessage(new AutodocToggleBrokenBoneBuiMsg());
    }

    public void ToggleOrganDamage()
    {
        SendMessage(new AutodocToggleOrganDamageBuiMsg());
    }

    public void StartSurgery()
    {
        SendMessage(new AutodocStartSurgeryBuiMsg());
    }

    public void Clear()
    {
        SendMessage(new AutodocClearBuiMsg());
    }

    public void Eject()
    {
        SendMessage(new AutodocEjectBuiMsg());
    }

    public void ImportScan()
    {
        SendMessage(new AutodocImportScanBuiMsg());
    }
}
