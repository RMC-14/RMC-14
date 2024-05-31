using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Medical.Surgery.Effects.Step;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedCMSurgerySystem))]
public sealed partial class CMSurgeryClampBleedEffectComponent : Component;
