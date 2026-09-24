using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.TrainingDummy;

[RegisterComponent]
[Access(typeof(RMCTrainingDummySystem))]
public sealed partial class RMCTrainingDummyComponent : Component
{
    [DataField]
    public ComponentRegistry? RemoveComponents;
}
