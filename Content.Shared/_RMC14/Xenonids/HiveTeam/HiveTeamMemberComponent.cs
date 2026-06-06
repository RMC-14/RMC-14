using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.HiveTeam;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HiveTeamMemberComponent : Component
{
    [DataField, AutoNetworkedField]
    public int TeamNumber;
}
