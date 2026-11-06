using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Acid;

/// <summary>
///     Active acid on damageable entities; weather can dilute exposed acid timers and damage once per storm.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedXenoAcidSystem), typeof(Content.Shared._RMC14.Weather.RMCWeatherSystem))]
public sealed partial class DamageableCorrodingComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Acid;

    [DataField, AutoNetworkedField]
    public XenoAcidStrength Strength = XenoAcidStrength.Normal;

    [DataField, AutoNetworkedField]
    public float Dps;

    [DataField, AutoNetworkedField]
    public DamageSpecifier Damage;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextDamageAt;

    [DataField, AutoNetworkedField]
    public TimeSpan AcidExpiresAt;

}
