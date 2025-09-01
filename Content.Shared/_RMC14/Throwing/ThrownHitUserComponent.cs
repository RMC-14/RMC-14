using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Throwing;

[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCThrowingSystem))]
public sealed partial class ThrownHitUserComponent : Component;
