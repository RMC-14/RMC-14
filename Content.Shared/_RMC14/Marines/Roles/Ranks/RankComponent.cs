using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Marines.Roles.Ranks;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRankSystem))]
public sealed partial class RankComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<RankPrototype>? Rank;
}