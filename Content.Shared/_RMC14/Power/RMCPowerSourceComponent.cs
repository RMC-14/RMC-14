using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Power;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCPowerSystem))]
public sealed partial class RMCPowerSourceComponent : Component
{
    [DataField, AutoNetworkedField]
    public RMCPowerSourceScope Scope;

    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    [DataField, AutoNetworkedField]
    public float AvailablePower;

    [DataField, AutoNetworkedField]
    public float CurrentPower;

    [ViewVariables]
    public EntityUid? Area;
}
