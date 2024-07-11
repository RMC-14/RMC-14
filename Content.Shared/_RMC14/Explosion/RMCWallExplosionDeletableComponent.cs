using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Explosion;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedCMExplosionSystem))]
public sealed partial class RMCWallExplosionDeletableComponent : Component;
