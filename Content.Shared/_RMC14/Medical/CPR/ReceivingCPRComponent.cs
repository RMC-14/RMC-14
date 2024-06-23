using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Medical.CPR;

[RegisterComponent, NetworkedComponent]
[Access(typeof(CPRSystem))]
public sealed partial class ReceivingCPRComponent : Component;
