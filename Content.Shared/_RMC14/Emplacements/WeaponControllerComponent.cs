using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Emplacements;

/// <summary>
///     Can be used to shoot a weapon remotely.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class WeaponControllerComponent : Component
{
    /// <summary>
    ///     The weapon entity that shoots at the position the component's owner is aiming at.
    /// </summary>
    [DataField, AutoNetworkedField]
    public NetEntity? ControlledWeapon;
}
