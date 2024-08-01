using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Bombard;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoBombardSystem))]
public sealed partial class XenoBombardComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Range = 10;

    [DataField, AutoNetworkedField]
    public FixedPoint2 PlasmaCost = 200;

    [DataField, AutoNetworkedField]
    public TimeSpan Delay = TimeSpan.FromSeconds(4.5);

    [DataField, AutoNetworkedField]
    public EntProtoId Projectile = "XenoBombardAcidProjectile";

    [DataField, AutoNetworkedField]
    public EntProtoId[] Projectiles = new[]
    {
        new EntProtoId("XenoBombardAcidProjectile"),
        new EntProtoId("XenoBombardNeurotoxinProjectile"),
    };

    [DataField, AutoNetworkedField]
    public SoundSpecifier PrepareSound = new SoundCollectionSpecifier("XenoDrool");

    [DataField, AutoNetworkedField]
    public SoundSpecifier ShootSound = new SoundPathSpecifier("/Audio/_RMC14/Xeno/blobattack.ogg");
}
