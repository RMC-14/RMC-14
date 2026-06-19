using Content.Shared.Damage;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared._RMC14.Xenonids.Projectile.Spit.Charge;

[Prototype]
public sealed partial class XenoAcidPrototype : IPrototype, IInheritingPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [ParentDataField(typeof(PrototypeIdArraySerializer<XenoAcidPrototype>))]
    public string[]? Parents { get; private set; }

    [AbstractDataField]
    [NeverPushInheritance]
    public bool Abstract { get; private set; }

    [DataField]
    public DamageSpecifier Damage = new();

    [DataField]
    public int Tier = 1;

    //TODO RMC14 Damage areas

    [DataField]
    public TimeSpan[] MultiplierThresholds = [TimeSpan.FromSeconds(21), TimeSpan.FromSeconds(9), TimeSpan.FromSeconds(20)];

    [DataField]
    public int ArmorPiercing;

    [DataField]
    public TimeSpan DurationBase = TimeSpan.FromSeconds(20);

    [DataField]
    public TimeSpan DurationAdd = TimeSpan.FromSeconds(10);

    [DataField]
    public TimeSpan MaxDuration = TimeSpan.FromSeconds(20);

    [DataField]
    public ProtoId<XenoAcidPrototype>? Upgrade;

    [DataField]
    public UserAcidedEffects Appearance;

    [DataField]
    public int WeakenArmor = 0;
}
