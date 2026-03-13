using Content.Shared._RMC14.Sentry;
using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Dropship.Utility.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCOrbitalDeployableComponent : Component
{
    /// <summary>
    ///     The prototype to deploy, if this is null the entity itself will be deployed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId? DeployPrototype;

    /// <summary>
    ///     Determines if the deployable falls slowly with a parachute or crashes to the floor.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool UseParachute = true;

    /// <summary>
    ///     The maximum amount of times the prototype can be deployed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MaxDeployCount = 1;

    /// <summary>
    ///     The remaining amount of times the prototype can be deployed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int RemainingDeployCount = 1;

    /// <summary>
    ///     The time it takes before the entity starts dropping on the target map.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ArrivingSoundDelay = 5;

    /// <summary>
    ///     The duration of the drop animation.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DropDuration = 3f;

    /// <summary>
    ///     The amount of damage dealt to entities near the target location when the drop is finished.
    ///     Only used if <see cref="DropPod"/> is false.
    /// </summary>
    [DataField, AutoNetworkedField]
    public DamageSpecifier? LandingDamage;

    /// <summary>
    ///     The range to check for entities with the <see cref="TurretComponent"/>, deployment is not possible if one is found.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int DefenseExclusionRange = 4;

    /// <summary>
    ///     Whether the deployed entity will be put in a drop pod before being dropped.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool DropPod = true;

    /// <summary>
    ///     The effect to display at the landing location during the drop.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId? LandingEffectId = "RMCEffectAlert";

    /// <summary>
    ///     The sound to play on the deployer's location when the deployer is activated.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? LaunchSound = new SoundPathSpecifier("/Audio/_RMC14/Effects/bamf.ogg");

    /// <summary>
    ///     The sound to play on the target location when the dropped entity start it's dropping animation.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? ArrivingSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/Techpod/techpod_drill.ogg");
}
