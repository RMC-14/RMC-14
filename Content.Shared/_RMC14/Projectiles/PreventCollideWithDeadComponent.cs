using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Projectiles;

[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCProjectileSystem))]
public sealed partial class PreventCollideWithDeadComponent : Component;
