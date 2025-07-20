using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Buckle;

[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCBuckleSystem))]
public sealed partial class RMCAllowStrapMovementComponent : Component;
