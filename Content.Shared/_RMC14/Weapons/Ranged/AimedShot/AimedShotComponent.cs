using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Weapons.Ranged.AimedShot;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AimedShotComponent : Component
{
    /// <summary>
    ///     The action prototype to add to entities holding this.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId ActionId = "RMCActionAimedShot";

    /// <summary>
    ///     The laser Prototype
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId LaserProto = "RMCSpottingLaser";

    /// <summary>
    ///     The sound to be played when the action is used.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier AimingSound = new SoundPathSpecifier("/Audio/_RMC14/Weapons/Handling/target_on.ogg");

    /// <summary>
    ///     The uid of the action.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Action;

    /// <summary>
    ///     If gunshots should be cancelled until aiming is done.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool WaitForAiming;

    /// <summary>
    ///     If the laser should be visible.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ShowLaser = true;

    /// <summary>
    ///     The base aiming duration.
    /// </summary>
    [DataField, AutoNetworkedField]
    public double AimDuration = 1.25;

    /// <summary>
    ///     The amount of time in seconds to be added to the aim duration for every tile of distance to the target.
    /// </summary>
    [DataField, AutoNetworkedField]
    public double AimDistanceDifficulty = 0.05;
}
