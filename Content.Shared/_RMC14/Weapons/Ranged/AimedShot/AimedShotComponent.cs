using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Weapons.Ranged.AimedShot;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(AimedShotSystem))]
public sealed partial class AimedShotComponent : Component
{
    /// <summary>
    ///     The action prototype to add to entities holding this.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId ActionId = "RMCActionAimedShot";

    /// <summary>
    ///     The action entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Action;

    /// <summary>
    ///     The sound to be played when the action is used.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier AimingSound = new SoundPathSpecifier("/Audio/_RMC14/Weapons/Handling/target_on.ogg");

    /// <summary>
    ///     If gunshots should be cancelled until aiming is done.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool WaitForAiming;

    /// <summary>
    ///     The target of the aimed shot.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Target;

    /// <summary>
    ///     The base aiming duration.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float AimDuration = 1.25f;

    /// <summary>
    ///     The amount of time in seconds to be added to the aim duration for every tile of distance to the target.
    /// </summary>
    [DataField, AutoNetworkedField]
    public double AimDistanceDifficulty = 0.05;

    /// <summary>
    ///     The whitelist required to use aimed shot.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist Whitelist = new();

    /// <summary>
    ///     The speed of the aimed shot projectile
    /// </summary>
    [DataField, AutoNetworkedField]
    public int ProjectileSpeed = 62;
}
