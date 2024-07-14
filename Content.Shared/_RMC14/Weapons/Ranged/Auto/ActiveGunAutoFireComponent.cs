using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Weapons.Ranged.Auto;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
[Access(typeof(GunToggleableAutoFireSystem))]
public sealed partial class ActiveGunAutoFireComponent : Component
{
    [DataField]
    public Vector2i Range = new(17, 10);

    [DataField, AutoPausedField]
    public TimeSpan NextFire;

    [DataField]
    public TimeSpan FailCooldown = TimeSpan.FromSeconds(0.1);

    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(2);
}
