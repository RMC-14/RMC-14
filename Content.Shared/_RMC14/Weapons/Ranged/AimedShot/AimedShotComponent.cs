using Content.Shared._RMC14.Targeting;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Weapons.Ranged.AimedShot;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCAimedShotSystem))]
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
    ///     If the aimed shot ability is on or off.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Activated;

    /// <summary>
    ///     The sound to be played when the action is used.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier AimingSound = new SoundPathSpecifier("/Audio/_RMC14/Weapons/Handling/target_on.ogg");

    /// <summary>
    ///     When the next aimed shot can be performed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan NextAimedShot =  TimeSpan.Zero;

    /// <summary>
    ///     The amount of time before another aimed shot can be performed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan AimedShotCooldown = TimeSpan.FromSeconds(2.5);

    /// <summary>
    ///     The range of the aimed shot action.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Range = 32;

    /// <summary>
    ///     The minimum distance to the target needed to perform the aimed shot action.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MinRange = 2;

    /// <summary>
    ///     A list of targets that are being aimed at.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<EntityUid> Targets = new();

    /// <summary>
    ///     The current target of the aimed shot.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? CurrentTarget;

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

    /// <summary>
    ///     The targeting effect to apply to entities being aimed at.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TargetedEffects TargetEffect = TargetedEffects.Targeted;

    /// <summary>
    ///     If the targeting visual should display a direction indicator while the laser is turned off.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ShowDirection = true;
}
