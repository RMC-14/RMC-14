using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Xenonids.Construction;

/// <summary>
/// Spreads hive weeds over time.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(HiveClusterSystem))]
[AutoGenerateComponentPause]
public sealed partial class HiveClusterComponent : Component
{
    /// <summary>
    /// Entity to spawn.
    /// </summary>
    [DataField]
    public EntProtoId Prototype = "XenoHiveWeeds";

    /// <summary>
    /// How long to wait between spreading.
    /// </summary>
    [DataField]
    public TimeSpan SpreadDelay = TimeSpan.FromSeconds(15);

    /// <summary>
    /// When weeds will next spread
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextSpread = TimeSpan.Zero;
}
