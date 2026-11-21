using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Paratoxin.ParatoxinSlashes;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoParatoxinSlashApplyComponent : Component
{
    [DataField, AutoNetworkedField]
    public int NumSlashes = 3;

    [DataField, AutoNetworkedField]
    public int StackAmount = 10;

    [DataField, AutoNetworkedField]
    public TimeSpan Duration = TimeSpan.FromSeconds(5);
}
