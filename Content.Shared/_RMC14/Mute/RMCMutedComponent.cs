using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Mute;

[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCMutedSystem))]
public sealed partial class RMCMutedComponent : Component;
