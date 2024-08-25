using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Pulling;

[RegisterComponent, NetworkedComponent]
[Access(typeof(CMPullingSystem))]
public sealed partial class ActivePreventPulledWhileAliveComponent : Component;
