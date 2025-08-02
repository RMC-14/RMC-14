using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Medical.Examine;

[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCMedicalExamineSystem))]
public sealed partial class RMCBlockMedicalExamineComponent : Component;
