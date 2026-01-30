using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Medical.Examine;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCMedicalExamineSystem))]
public sealed partial class RMCMedicalExamineComponent : Component
{
    [DataField, AutoNetworkedField]
    public LocId UnrevivableText = "rmc-medical-examine-unrevivable";

    [DataField, AutoNetworkedField]
    public LocId AliveText = "rmc-medical-examine-alive";

    [DataField, AutoNetworkedField]
    public LocId DeadText = "rmc-medical-examine-dead";

    [DataField, AutoNetworkedField]
    public LocId CritText = "rmc-medical-examine-unconscious";

    [DataField, AutoNetworkedField]
    public LocId BleedText = "rmc-medical-examine-bleeding";

    [DataField, AutoNetworkedField]
    public bool Simple = false;
}
