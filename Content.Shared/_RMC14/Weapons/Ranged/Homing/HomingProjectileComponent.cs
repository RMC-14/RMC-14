using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Weapons.Ranged.Homing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState()]
public sealed partial class HomingProjectileComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Target;

    [DataField, AutoNetworkedField]
    public int ProjectileSpeed = 62;
}
