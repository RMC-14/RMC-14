using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Explosion;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMClusterGrenadeSystem))]
public sealed partial class ClusterLimitHitsComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Limit = 1;
}
