using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Emplacements;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState()]
public sealed partial class MountableWeaponComponent : Component
{
    /// <summary>
    ///     Whether the weapon can only be used while attached to a mount.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool RequiresMount = true;

    /// <summary>
    ///     The number of free hands required to shoot the weapon.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int RequiredFreeHands = 2;

    /// <summary>
    ///     The mount entity the weapon is attached to.
    /// </summary>
    [DataField, AutoNetworkedField]
    public NetEntity? MountedTo;

    /// <summary>
    ///     The firing arc, in degrees, within which the weapon can shoot while attached to a mount.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int ShootArc = 160;
}
