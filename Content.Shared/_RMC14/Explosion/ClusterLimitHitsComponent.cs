using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Explosion;

[RegisterComponent, NetworkedComponent]
[Access(typeof(CMClusterGrenadeSystem))]
public sealed partial class ClusterLimitHitsComponent : Component;
