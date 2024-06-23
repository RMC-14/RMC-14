using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Projectiles;

[RegisterComponent, NetworkedComponent]
[Access(typeof(CMProjectileSystem))]
public sealed partial class DeleteOnCollideComponent : Component;
