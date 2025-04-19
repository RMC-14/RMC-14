using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Projectile.Spit.Scattered;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoSpitSystem))]
public sealed partial class XenoScatteredSpitComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 PlasmaCost = 30;

    [DataField, AutoNetworkedField]
    public float Speed = 30;

    [DataField, AutoNetworkedField]
    public EntProtoId ProjectileId = "XenoScatteredSpitProjectile";

    [DataField, AutoNetworkedField]
    public SoundSpecifier Sound = new SoundCollectionSpecifier("XenoSpitAcid", AudioParams.Default.WithVolume(-10f));

    [DataField, AutoNetworkedField]
    public int MaxProjectiles = 5;

    [DataField, AutoNetworkedField]
    public Angle MaxDeviation = Angle.FromDegrees(45);
}
