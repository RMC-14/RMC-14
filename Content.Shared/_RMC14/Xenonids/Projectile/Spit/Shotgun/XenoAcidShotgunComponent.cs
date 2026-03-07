using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Projectile.Spit.Shotgun;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoSpitSystem))]

public sealed partial class XenoAcidShotgunComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 PlasmaCost = 60;

    [DataField, AutoNetworkedField]
    public float Speed = 40;

    [DataField, AutoNetworkedField]
    public EntProtoId ProjectileId = "XenoAcidShotgunProjectile";

    [DataField, AutoNetworkedField]
    public SoundSpecifier Sound = new SoundCollectionSpecifier("XenoSpitAcid", AudioParams.Default.WithVolume(-5f));

    [DataField, AutoNetworkedField]
    public int MaxProjectiles = 9;

    [DataField, AutoNetworkedField]
    public Angle MaxDeviation = Angle.FromDegrees(30);
}
