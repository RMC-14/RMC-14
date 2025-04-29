using Content.Shared.Item;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Webbing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedWebbingSystem))]
public sealed partial class WebbingClothingComponent : Component
{
    [DataField, AutoNetworkedField]
    public string Container = "cm_clothing_webbing_slot";

    [DataField, AutoNetworkedField]
    public EntityUid? Webbing;

    /// <summary>
    /// The item size this piece of clothing had without webbing.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<ItemSizePrototype>? UnequippedSize;

    [DataField, AutoNetworkedField]
    public EntProtoId<WebbingComponent>? StartingWebbing;
}
