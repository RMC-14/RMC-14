using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Medical.Surgery.Steps.XenoParts;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedCMSurgerySystem))]
public sealed partial class RMCXenoRemovedComponent : Component;
