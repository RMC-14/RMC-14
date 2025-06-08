using Content.Shared._RMC14.Item;
using Content.Shared.Roles;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Rules;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCPlanetSystem))]
public sealed partial class RMCPlanetMapPrototypeComponent : Component
{
    [DataField(required: true), AutoNetworkedField, Access(Other = AccessPermissions.ReadExecute)]
    public ResPath Map;

    [DataField, AutoNetworkedField]
    public CamouflageType Camouflage = CamouflageType.Jungle;

    [DataField, AutoNetworkedField]
    public int MinPlayers;

    [DataField, AutoNetworkedField]
    public int MaxPlayers;

    [DataField(required: true), AutoNetworkedField]
    public string Announcement = string.Empty;

    [DataField, AutoNetworkedField]
    public List<(ProtoId<JobPrototype> Job, int Amount)>? SurvivorJobs;

    /// <summary>
    /// Will override a preferred job to a list of other variants
    /// For example, if you have security survivor selected it will pick one of the inserts
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<JobPrototype>, List<(ProtoId<JobPrototype> Insert, int Amount)>>? SurvivorJobInserts;

    /// <summary>
    /// Will override a preferred job to another
    /// Useful for FORECON so any survivor preference will be overriden to FORECON survivor
    /// Basically, if security survivor is overriden by forecon survivor, it will be as if sec survivor: high is forecon survivor: high
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<JobPrototype>, ProtoId<JobPrototype>>? SurvivorJobOverrides;

    /// <summary>
    /// Instead of using the limits of the insert, this will select a random insert and use the base job's limit when true.
    /// If it is false, it will use the job slots of the insert. See Chance's Claim.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool SelectRandomSurvivorInsert = true;
}
