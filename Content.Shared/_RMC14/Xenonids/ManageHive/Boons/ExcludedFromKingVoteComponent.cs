using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.ManageHive.Boons;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(HiveBoonSystem))]
public sealed partial class ExcludedFromKingVoteComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool CanVote;

    [DataField, AutoNetworkedField]
    public bool CanBeKing;
}
