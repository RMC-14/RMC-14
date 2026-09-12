using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Power;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCPowerSystem))]
public sealed partial class RMCPowerStorageComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool InputEnabled;

    [DataField, AutoNetworkedField]
    public float InputLimit = 200_000;

    [DataField, AutoNetworkedField]
    public float MaxInput = 200_000;

    [DataField, AutoNetworkedField]
    public bool OutputEnabled = true;

    [DataField, AutoNetworkedField]
    public float OutputLimit = 50_000;

    [DataField, AutoNetworkedField]
    public float MaxOutput = 200_000;

    [DataField, AutoNetworkedField]
    public float CurrentInput;

    [DataField, AutoNetworkedField]
    public float CurrentOutput;

    [DataField, AutoNetworkedField]
    public RMCPowerStorageInputState InputState;

    [ViewVariables]
    public EntityUid? Area;
}
