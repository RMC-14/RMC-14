using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Construction.ResinHole;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class InResinHoleRangeComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<EntityUid> HoleList = new();
}