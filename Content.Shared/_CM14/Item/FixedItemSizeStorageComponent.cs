using Content.Shared.Item;
using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Item;

// TODO CM14 rename to slot storage
// TODO CM14 upstream this
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedItemSystem))]
public sealed partial class FixedItemSizeStorageComponent : Component
{
    [DataField, AutoNetworkedField]
    public Vector2i Size = new(2, 2);

    public Box2i[]? CachedSize;
}
