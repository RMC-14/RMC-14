using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Weapons.Ranged.Auto;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
[Access(typeof(GunToggleableAutoFireSystem))]
public sealed partial class ActiveGunAutoFireComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextFire;

    [DataField]
    public TimeSpan FailCooldown = TimeSpan.FromSeconds(0.2);

    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(2);
}
