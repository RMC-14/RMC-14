using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Hands;

[RegisterComponent, NetworkedComponent]
[Access(typeof(CMHandsSystem))]
public sealed partial class RMCStorageEjectHandComponent : Component;
