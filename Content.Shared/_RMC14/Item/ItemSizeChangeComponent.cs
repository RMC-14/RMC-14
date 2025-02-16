using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Item;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ItemSizeChangeSystem))]
public sealed partial class ItemSizeChangeComponent : Component
{
    [DataField, AutoNetworkedField]
    public int? BaseSize;
}
