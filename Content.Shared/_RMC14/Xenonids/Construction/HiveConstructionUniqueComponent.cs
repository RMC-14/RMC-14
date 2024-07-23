using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Construction;

/// <summary>
/// When this construct is built it will require a free construction slot.
/// When deleted, it will free a construction slot of this id in the hive.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedXenoConstructionSystem))]
public sealed partial class HiveConstructionUniqueComponent : Component
{
    [DataField(required: true)]
    public EntProtoId Id = string.Empty;
}
