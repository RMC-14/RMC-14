using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Xenos.Projectile;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoProjectileSystem))]
public sealed partial class XenoProjectileComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool DeleteOnFriendlyXeno;

    [DataField, AutoNetworkedField]
    public EntityUid? Hive;
}
