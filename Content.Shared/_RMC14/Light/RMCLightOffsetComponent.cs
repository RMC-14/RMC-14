using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Light;

[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCLightOffsetSystem))]
public sealed partial class RMCLightOffsetComponent : Component;
