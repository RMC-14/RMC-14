using Content.Shared._RMC14.Power;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._RMC14.Power;

[UsedImplicitly]
public sealed class RMCPortableGeneratorBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{

    [ViewVariables]
    private RMCPortableGeneratorWindow? _window;

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<RMCPortableGeneratorWindow>();
        _window.SetOwner(Owner);

        _window.ToggleButton.OnPressed += _ => SendPredictedMessage(new RMCPortableGeneratorToggleBuiMsg());
        _window.EjectButton.OnPressed += _ => SendPredictedMessage(new RMCPortableGeneratorEjectFuelBuiMsg());
        _window.RaisePowerButton.OnPressed += _ => SendPredictedMessage(new RMCPortableGeneratorRaisePowerBuiMsg());
        _window.LowerPowerButton.OnPressed += _ => SendPredictedMessage(new RMCPortableGeneratorLowerPowerBuiMsg());

    }

}
