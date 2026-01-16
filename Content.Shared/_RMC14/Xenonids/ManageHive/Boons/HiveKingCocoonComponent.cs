using Content.Shared.Roles;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.ManageHive.Boons;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(HiveBoonSystem))]
public sealed partial class HiveKingCocoonComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan TimeLeft = TimeSpan.FromMinutes(10);

    [DataField, AutoNetworkedField]
    public int LastPylons;

    [DataField, AutoNetworkedField]
    public int RequiredPylons = 2;

    [DataField, AutoNetworkedField]
    public bool FirstWarning;

    [DataField, AutoNetworkedField]
    public bool VoteStarted;

    [DataField, AutoNetworkedField]
    public bool FinalWarning;

    [DataField, AutoNetworkedField]
    public EntProtoId Spawn = "RMCXenoKing";
}
