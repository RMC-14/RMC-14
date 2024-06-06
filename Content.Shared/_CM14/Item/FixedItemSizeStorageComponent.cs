using Content.Shared.Item;
using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Item;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedItemSystem))]
public sealed partial class FixedItemSizeStorageComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public Vector2i Size;

    public Box2i[]? CachedSize;
}
