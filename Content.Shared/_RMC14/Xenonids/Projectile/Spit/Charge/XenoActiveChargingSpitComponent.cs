using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Xenonids.Projectile.Spit.Charge;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(XenoSpitSystem))]
public sealed partial class XenoActiveChargingSpitComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan ExpiresAt;

    [DataField]
    public DamageSpecifier Damage = new();

    [DataField, AutoNetworkedField]
    public TimeSpan ProjectileLifetime;
}
