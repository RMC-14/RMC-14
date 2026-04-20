using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Targeting;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState()]
public sealed partial class TargetingLaserComponent : Component
{
    /// <summary>
    ///     If the laser is supposed to be visible.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ShowLaser = true;

    /// <summary>
    ///     The laser type.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TargetingLaserType LaserType;

    /// <summary>
    ///     The default alpha multiplier of any lasers originating from this entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float LaserAlpha = 0.5f;

    /// <summary>
    ///     If the laser alpha should be based on how long the targeting has lasted.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool GradualAlpha = true;

    /// <summary>
    ///     The width of the laser.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float LaserWidth = 0.3f;

    [DataField]
    public ResPath RsiPath = new("/Textures/_RMC14/Effects/beam.rsi");

    [DataField]
    public string LaserState = "laser_beam";

    [DataField]
    public string LaserIntenseState = "laser_beam_intense";
}

[Serializable, NetSerializable]
public enum TargetingLaserType
{
    Normal,
    Intense,
}
