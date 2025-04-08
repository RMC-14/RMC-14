using Robust.Shared.GameStates;

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
    ///     The original color of the laser.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Color LaserColor = Color.Red;

    /// <summary>
    ///     The current color of the laser
    /// </summary>
    [DataField, AutoNetworkedField]
    public Color CurrentLaserColor = Color.Red;

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
}
