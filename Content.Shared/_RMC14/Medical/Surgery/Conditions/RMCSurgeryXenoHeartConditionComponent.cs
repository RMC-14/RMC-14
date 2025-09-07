using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Medical.Surgery.Conditions;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedCMSurgerySystem))]
public sealed partial class RMCSurgeryXenoHeartConditionComponent : Component;
