using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.Roles;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Storage;

/// <summary>
/// Fills an entity storage with items from an entity table when hijack starts.
/// Optionally scales with the number of active players with the specified jobs.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCStorageSystem))]
public sealed partial class RMCOnHijackFillComponent : Component
{
    [DataField]
    public List<ProtoId<JobPrototype>> Jobs = [];

    [DataField]
    public EntityTableSelector? Table;

    [DataField]
    public bool ScaleWithJobs;
}
