using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Projectile;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoProjectileSystem))]
public sealed partial class XenoProjectileShooterComponent : Component
{
    [DataField, AutoNetworkedField]
    public int NextId;

    [DataField, AutoNetworkedField]
    public List<EntityUid> Shot = new();
}
