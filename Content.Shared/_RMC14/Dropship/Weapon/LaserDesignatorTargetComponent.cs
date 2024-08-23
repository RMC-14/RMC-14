using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Dropship.Weapon;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedDropshipWeaponSystem))]
public sealed partial class LaserDesignatorTargetComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Id;

    [DataField, AutoNetworkedField]
    public EntityUid? LaserDesignator;
}
