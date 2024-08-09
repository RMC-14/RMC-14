using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Construction;

/// <summary>
/// When this construct is created, increases the hive's construct limit by some amount.
/// If it is destroyed the limits are decreased but any old structures stay grandfathered in,
/// but they cannot be rebuilt if they get destroyed as well.
/// </summary>
[RegisterComponent, Access(typeof(ModifyHiveLimitsSystem))]
public sealed partial class ModifyHiveLimitsComponent : Component
{
    /// <summary>
    /// Construction limits to modify.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<EntProtoId, int> Construction = new();
}
