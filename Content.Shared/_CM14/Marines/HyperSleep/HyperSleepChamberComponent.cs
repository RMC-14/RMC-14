using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Marines.HyperSleep;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class HyperSleepChamberComponent : Component
{
    [DataField, AutoNetworkedField]
    public string ContainerId = "cm_hyper_sleep";
}
