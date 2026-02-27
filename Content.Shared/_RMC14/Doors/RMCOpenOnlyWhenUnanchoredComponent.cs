using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Doors;

[RegisterComponent, NetworkedComponent]
[Access(typeof(CMDoorSystem))]
public sealed partial class RMCOpenOnlyWhenUnanchoredComponent : Component;
