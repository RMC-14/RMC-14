using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Buckle;

[RegisterComponent, NetworkedComponent]
[Access(typeof(CMBuckleSystem))]
public sealed partial class BuckleClimbableComponent : Component;
