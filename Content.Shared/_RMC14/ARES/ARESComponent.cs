using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.ARES;

[RegisterComponent, NetworkedComponent]
[Access(typeof(ARESSystem))]
public sealed partial class ARESComponent : Component;
