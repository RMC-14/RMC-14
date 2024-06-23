using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Medical.Surgery.Effects.Complete;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedCMSurgerySystem))]
public sealed partial class CMSurgeryRemoveLarvaComponent : Component;
