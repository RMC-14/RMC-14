using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Input;

[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCInputSystem))]
public sealed partial class ActiveInputMoverComponent : Component;
