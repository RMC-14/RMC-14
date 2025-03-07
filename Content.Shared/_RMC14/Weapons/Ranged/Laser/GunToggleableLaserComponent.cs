using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Weapons.Ranged.Laser;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GunToggleableLaserComponent : Component
{
    /// <summary>
    ///     Is the laser active
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Active = true;

    /// <summary>
    ///     The action prototype belonging to this action.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId ActionId = "RMCActionToggleLaser";

    /// <summary>
    ///     The action id.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Action;

    /// <summary>
    ///     The sound to play when this action is used.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier ToggleSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/click.ogg");

    /// <summary>
    ///     The duration multiplier to apply during aimed shot while the laser is active.
    /// </summary>
    [DataField, AutoNetworkedField]
    public double AimDurationMultiplier = 0.6;

    /// <summary>
    ///     The value to subtract from the duration multiplier if the laser is active and the target is spotted.
    /// </summary>
    [DataField, AutoNetworkedField]
    public double SpottedAimDurationMultiplierSubtraction = 0.15;
}
