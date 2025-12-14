using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Medical.Refill;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMRefillableSolutionSystem))]
public sealed partial class RMCSmartRefillTankComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public string Solution = string.Empty;
}
