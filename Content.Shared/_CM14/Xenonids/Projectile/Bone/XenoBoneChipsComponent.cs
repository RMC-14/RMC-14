using Content.Shared._CM14.Xenonids.Projectile.Spit;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CM14.Xenonids.Projectile.Bone;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoSpitSystem))]
public sealed partial class XenoBoneChipsComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Speed = 6;

    [DataField, AutoNetworkedField]
    public EntProtoId ProjectileId = "XenoBoneChipsProjectile";
}
