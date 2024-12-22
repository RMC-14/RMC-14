using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Rank;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoRankComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Rank;
}
