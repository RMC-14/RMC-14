using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Targeting;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState()]
public sealed partial class TargetingLaserComponent : Component
{
    /// <summary>
    ///     The target of the laser
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid Target;

    /// <summary>
    ///     The entity where the laser is originating from
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid Origin;

    /// <summary>
    ///     If the laser is supposed to be visible.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ShowLaser = true;

    /// <summary>
    ///     The speed of the aimed shot projectile
    /// </summary>
    [DataField, AutoNetworkedField]
    public Color LaserColor = Color.Red;

    /// <summary>
    ///     How long the spotting should last if not interrupted.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float LaserAlpha = 0.5f;

    /// <summary>
    ///     If the laser alpha should be based on how long the aiming has lasted.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool GradualAlpha = true;
}
