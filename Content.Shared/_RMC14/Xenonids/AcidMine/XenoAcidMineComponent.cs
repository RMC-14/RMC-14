using Content.Shared._RMC14.Xenonids.DeployTraps;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.AcidMine;
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoAcidMineSystem), typeof(XenoDeployTrapsSystem))]
public sealed partial class XenoAcidMineComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public DamageSpecifier BaseDamage;

    [DataField, AutoNetworkedField]
    public bool Empowered = false;

    [DataField, AutoNetworkedField]
    public int Range = 10;

    [DataField]
    public DoAfterId? AcidMineDoAfter;

    [DataField]
    public FixedPoint2 PlasmaCost = 40;

    //1 for a 3x3 area.
    [DataField, AutoNetworkedField]
    public int AcidMineRadius = 1;

    // Length of do-after
    [DataField, AutoNetworkedField]
    public TimeSpan Delay = TimeSpan.FromSeconds(1.35);

    //applied dot acid values (only applied when empowered)
    [DataField, AutoNetworkedField]
    public TimeSpan AcidDuration = TimeSpan.FromSeconds(20);

    [DataField, AutoNetworkedField]
    public TimeSpan AcidProlongDuration = TimeSpan.FromSeconds(5);

    [DataField, AutoNetworkedField]
    public DamageSpecifier AcidDamage = new();

    [DataField, AutoNetworkedField]
    public int AcidArmorPiercing = 40;

    [DataField, AutoNetworkedField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(5.5);

    [DataField, AutoNetworkedField]
    public TimeSpan DeployTrapsCooldownReduction = TimeSpan.FromSeconds(4);

    [DataField, AutoNetworkedField]
    public EntProtoId TelegraphEffect = "RMCEffectXenoTelegraphRedSlow";

    [DataField, AutoNetworkedField]
    public SoundSpecifier SizzleSound = new SoundCollectionSpecifier("XenoAcidSizzle");

    [DataField, AutoNetworkedField]
    public SoundSpecifier ExplosionSound = new SoundPathSpecifier("/Audio/_RMC14/Effects/meteorimpact.ogg");

    [DataField, AutoNetworkedField]
    public EntProtoId SmokeEffect = "XenoAcidExplosionEffect";
}
