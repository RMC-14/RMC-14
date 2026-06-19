using Content.Shared._RMC14.Xenonids.AcidMine;
using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Xenonids.Projectile.Spit.Charge;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(XenoSpitSystem), typeof(XenoAcidBlastSystem))]
public sealed partial class UserAcidedComponent : Component
{
    [DataField, AutoNetworkedField]
    public DamageSpecifier Damage = new();

    [DataField, AutoNetworkedField]
    public int ArmorPiercing;

    [DataField, AutoNetworkedField]
    public TimeSpan MaxDuration;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan? ExpiresAt;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextDamageAt;

    [DataField, AutoNetworkedField]
    public TimeSpan DamageEvery = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public TimeSpan ResistDuration = TimeSpan.FromSeconds(3);

    [DataField, AutoNetworkedField]
    public int Tier;

    [DataField, AutoNetworkedField]
    public TimeSpan[] MultiplierThresholds;

    [DataField, AutoNetworkedField]
    public TimeSpan? NextMultThreshold;

    [DataField, AutoNetworkedField]
    public int DamageMultiplier = 1;

    [DataField, AutoNetworkedField]
    public TimeSpan AllowVaporHitAfter;

    [DataField, AutoNetworkedField]
    public TimeSpan ExtinguishGracePeriod = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public UserAcidedEffects Appearance = UserAcidedEffects.Weak;

    [DataField, AutoNetworkedField]
    public int WeakenArmor = 0;

    [DataField, AutoNetworkedField]
    public TimeSpan ExtinguishAmount = TimeSpan.FromSeconds(27);

    [DataField, AutoNetworkedField]
    public ProtoId<XenoAcidPrototype>? Upgrade;
}

[Serializable, NetSerializable]
public enum UserAcidedVisuals
{
    Acided,
}

[Serializable, NetSerializable]
public enum UserAcidedEffects
{
    None,
    Weak,
    Normal,
    Strong
}
