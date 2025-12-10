using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Entrenching;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(BarricadeSystem))]
public sealed partial class BarricadeComponent : Component
{
    /// <summary>
    /// How much coverage this barricade provides against projectiles. 100 for full coverage.
    /// This value is in percents.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ProjectileCoverage = 85;

    /// <summary>
    /// Number of tiles needed to max out block probability.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DistanceLimit = 6.5f;

    /// <summary>
    /// Degree to which accuracy affects probability.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float AccuracyFactor = 50;
}
