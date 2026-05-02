using Content.Shared._RMC14.SupplyDrop;
using Content.Shared.Damage;
using Content.Shared.Whitelist;
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
    ///     If <see cref="DropPod"/> is true, the landing damage specified on the drop pod prototype's <see cref="SupplyDropPodComponent"/> will be used instead.
    /// </summary>
    [DataField, AutoNetworkedField]
    public DamageSpecifier? LandingDamage;

    /// <summary>
    ///     The whitelist to check for in the <see cref="DefenseExclusionRange"/>, deployment is not possible if any found entity matches the whitelist.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? DeployBlacklist;

    /// <summary>
    ///     The range to check for entities matching the <see cref="DeployBlacklist"/>.
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
    ///     If <see cref="DropPod"/> is true, the arriving sound stored on the drop pod prototype's <see cref="SupplyDropPodComponent"/> will be used instead.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? ArrivingSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/Techpod/techpod_drill.ogg");
}
