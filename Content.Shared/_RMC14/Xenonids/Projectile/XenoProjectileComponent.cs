using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Projectile;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoProjectileSystem))]
public sealed partial class XenoProjectileComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool DeleteOnFriendlyXeno;
}
