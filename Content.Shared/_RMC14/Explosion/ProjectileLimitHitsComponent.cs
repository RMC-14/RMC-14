using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Explosion;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMClusterGrenadeSystem))]
public sealed partial class ProjectileLimitHitsComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Id;

    [DataField, AutoNetworkedField]
    public int Limit = 1;
}
