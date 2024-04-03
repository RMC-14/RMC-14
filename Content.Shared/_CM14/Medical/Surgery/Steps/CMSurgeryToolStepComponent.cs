using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CM14.Medical.Surgery.Steps;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedCMSurgerySystem))]
public sealed partial class CMSurgeryToolStepComponent : Component
{
    [DataField(required: true)]
    public ComponentRegistry Tool = new();

    [DataField]
    public ComponentRegistry? Add;

    [DataField]
    public ComponentRegistry? Remove;
}
