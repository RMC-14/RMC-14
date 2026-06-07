using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.HiveLeader;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(HiveLeaderSystem))]
public sealed partial class HiveLeaderComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Granter;

    [DataField, AutoNetworkedField]
    public string PheromonesContainerId = "rmc_hive_leader_pheromones";

    [DataField, AutoNetworkedField]
    public TimeSpan FriendlyStunTime = TimeSpan.FromSeconds(1.25);

    [DataField, AutoNetworkedField]
    public int? GrantRadioTextIncrease = 2;

    [DataField, AutoNetworkedField]
    public LocId LeaderPrefix = "rmc-xeno-name-leader";

    [DataField, AutoNetworkedField]
    public EntProtoId SquadActionId = "ActionXenoHiveLeaderSquad";

    [DataField, AutoNetworkedField]
    public EntityUid? SquadAction;
}
