using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Item;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(MultiHandedHolderSystem))]
public sealed partial class MultiHandedHolderComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<MultiHandedItem> Items = new();
}
