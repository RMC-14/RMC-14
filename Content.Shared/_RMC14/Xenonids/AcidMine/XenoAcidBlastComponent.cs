using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.AcidMine;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoAcidBlastSystem), typeof(XenoAcidMineSystem))]
public sealed partial class XenoAcidBlastComponent : Component
{
    // The xeno who cast this, for cooldown reduction
    [DataField, AutoNetworkedField]
    public EntityUid? Attached;

    [DataField, AutoNetworkedField]
    public TimeSpan Delay = TimeSpan.FromSeconds(1.35);

    [DataField, AutoNetworkedField]
    public TimeSpan Activation = TimeSpan.Zero;

    [DataField, AutoNetworkedField]
    public bool Activated;

    [DataField, AutoNetworkedField]
    public float BlastRadius = 0.5f;

    [DataField, AutoNetworkedField]
    public DamageSpecifier BaseDamage = new();

    [DataField, AutoNetworkedField]
    public bool Empowered;

    [DataField, AutoNetworkedField]
    public TimeSpan AcidDuration = TimeSpan.FromSeconds(20);

    [DataField, AutoNetworkedField]
    public TimeSpan AcidProlongDuration = TimeSpan.FromSeconds(5);

    [DataField, AutoNetworkedField]
    public DamageSpecifier AcidDamage = new();

    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> AlreadyHit = new();

    [DataField, AutoNetworkedField]
    public int AcidArmorPiercing = 40;

    [DataField, AutoNetworkedField]
    public TimeSpan DeployTrapsCooldownReduction = TimeSpan.FromSeconds(4);

    [DataField, AutoNetworkedField]
    public EntProtoId TelegraphEffect = "RMCEffectXenoTelegraphYellowPulse";

    [DataField, AutoNetworkedField]
    public SoundSpecifier SizzleSound = new SoundCollectionSpecifier("XenoAcidSizzle");

    [DataField, AutoNetworkedField]
    public SoundSpecifier ExplosionSound = new SoundPathSpecifier("/Audio/_RMC14/Effects/meteorimpact.ogg");

    [DataField, AutoNetworkedField]
    public EntProtoId SmokeEffect = "XenoAcidExplosionEffect";

    [DataField, AutoNetworkedField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(5.5);
}
