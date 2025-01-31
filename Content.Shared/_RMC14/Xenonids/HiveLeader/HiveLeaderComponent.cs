﻿using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.HiveLeader;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(HiveLeaderSystem))]
public sealed partial class HiveLeaderComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Granter;

    [DataField, AutoNetworkedField]
    public string PheromonesContainerId = "rmc_hive_leader_pheromones";
}
