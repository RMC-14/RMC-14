using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared._RMC14.Targeting;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCTargetingSystem))]
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
    ///     The origin coordinates of the laser
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityCoordinates Origin;

    /// <summary>
    ///     The remaining durations of the lasers.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<EntityUid,List<float>> LaserDurations = new();

    /// <summary>
    ///     The original durations of the lasers.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<EntityUid,List<float>> OriginalLaserDurations = new();

    /// <summary>
    ///     The visualiser to enable on the entity being targeted by the laser.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TargetedEffects LaserType;
}
