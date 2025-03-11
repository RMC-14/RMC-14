using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Targeting;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(TargetingSystem))]
public sealed partial class TargetingComponent : Component
{
    /// <summary>
    ///     The entity creating the laser.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid Source;

    /// <summary>
    ///     A list of targets targeted by this entity
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<EntityUid> Targets = new();

    /// <summary>
    ///     The entity using the entity that creates the laser.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid User;

    /// <summary>
    ///     A list of the laser entities.
    /// </summary>
    [DataField]
    public List<EntityUid> Laser = new();

    /// <summary>
    ///     The laser prototype to spawn.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId LaserProto = "RMCSpottingLaser";

    /// <summary>
    ///     The origin coordinates of the laser
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityCoordinates Origin;

    /// <summary>
    ///     The remaining duration of the laser
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<float> LaserDurations = new();

    /// <summary>
    ///     The original duration of the laser
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<float> OriginalLaserDurations = new();

    /// <summary>
    ///     The visualiser to enable on the entity being targeted by the laser
    /// </summary>
    [DataField, AutoNetworkedField]
    public TargetedEffects LaserType;

    /// <summary>
    ///     If the laser should be visible
    /// </summary>
    [DataField, AutoNetworkedField]
    public float AlphaMultiplier = 1f;

    /// <summary>
    ///     If the laser alpha should be based on how long the aiming has lasted.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool GradualAlpha;
}

