using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Marines.Roles.Ranks;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedRankSystem))]
public sealed partial class RankComponent : Component
{
    [DataField]
    public ProtoId<RankPrototype>? Rank;
}