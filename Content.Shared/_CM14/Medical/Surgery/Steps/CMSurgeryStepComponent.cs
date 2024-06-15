using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CM14.Medical.Surgery.Steps;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedCMSurgerySystem))]
[EntityCategory("SurgerySteps")]
public sealed partial class CMSurgeryStepComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Skill = 1;

    [DataField]
    public ComponentRegistry? Tool;

    [DataField]
    public ComponentRegistry? Add;

    [DataField]
    public ComponentRegistry? Remove;

    [DataField]
    public ComponentRegistry? BodyRemove;
}
