using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Rangefinder.Spotting;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState()]
public sealed partial class ActiveTargetingLaserComponent : Component
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
    ///     The duration of the laser
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<double> LaserDurations = new();

    /// <summary>
    ///     The visualiser to enable on the entity being targeted by the laser
    /// </summary>
    [DataField, AutoNetworkedField]
    public TargetedEffects LaserType;

    /// <summary>
    ///     If the laser should be visible
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ShowLaser;
}

