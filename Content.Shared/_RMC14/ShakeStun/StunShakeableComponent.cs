using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.ShakeStun;

[RegisterComponent, NetworkedComponent]
[Access(typeof(StunShakeableSystem))]
public sealed partial class StunShakeableComponent : Component;
