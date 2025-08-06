using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Xenonids.Projectile.Spit.Charge;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(XenoSpitSystem))]
public sealed partial class UserAcidedComponent : Component
{
    [DataField, AutoNetworkedField]
    public DamageSpecifier Damage = new();

    [DataField, AutoNetworkedField]
    public int ArmorPiercing;

    [DataField, AutoNetworkedField]
    public TimeSpan Duration = TimeSpan.FromSeconds(20);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan ExpiresAt;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextDamageAt;

    [DataField, AutoNetworkedField]
    public TimeSpan DamageEvery = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public bool Combo;

    [DataField, AutoNetworkedField]
    public TimeSpan ResistDuration = TimeSpan.FromSeconds(3);

    [DataField, AutoNetworkedField]
    public int ResistsNeeded = 1;

    [DataField, AutoNetworkedField]
    public TimeSpan AllowVaporHitAfter;

    [DataField, AutoNetworkedField]
    public TimeSpan ExtinguishGracePeriod = TimeSpan.FromSeconds(1);
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
    Normal,
    Enhanced,
}
