using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Medical.Surgery.Steps;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedCMSurgerySystem))]
public sealed partial class CMSurgeryCutLarvaRootsStepComponent : Component;
