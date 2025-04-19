using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Physics;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Damage;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCDamageableSystem))]
public sealed partial class DamageOverTimeComponent : Component
{
    [DataField, AutoNetworkedField]
    public DamageSpecifier? Damage;

    [DataField, AutoNetworkedField]
    public DamageSpecifier? ArmorPiercingDamage;

    [DataField, AutoNetworkedField]
    public DamageSpecifier? BarricadeDamage;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? BarricadeSound = new SoundCollectionSpecifier("XenoAcidSizzle", AudioParams.Default.WithVolume(-3));

    [DataField, AutoNetworkedField]
    public SoundSpecifier? Sound;

    [DataField, AutoNetworkedField]
    public TimeSpan DamageEvery = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public TimeSpan NextDamageAt;

    [DataField, AutoNetworkedField]
    public bool AffectsDead;

    [DataField, AutoNetworkedField]
    public bool AffectsInfectedNested;

    [DataField, AutoNetworkedField]
    public List<ProtoId<EmotePrototype>>? Emotes = new() { "Cough" };

    [DataField, AutoNetworkedField]
    public string? Popup;

    [DataField, AutoNetworkedField]
    public EntityWhitelist? Whitelist;

    [DataField, AutoNetworkedField]
    public List<DamageMultiplier>? Multipliers;

    [DataField, AutoNetworkedField]
    public CollisionGroup Collision = CollisionGroup.MobLayer | CollisionGroup.MobMask;

    [DataField, AutoNetworkedField]
    public bool InitDamaged;

    [DataField, AutoNetworkedField]
    public EntProtoId? DuplicateId;

    [DataRecord]
    [Serializable, NetSerializable]
    public readonly record struct DamageMultiplier(FixedPoint2 Multiplier, EntityWhitelist Whitelist);
}
