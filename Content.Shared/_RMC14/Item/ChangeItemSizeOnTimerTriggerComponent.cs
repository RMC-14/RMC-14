using Content.Shared.Item;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Item;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ItemSizeChangeSystem))]
public sealed partial class ChangeItemSizeOnTimerTriggerComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<ItemSizePrototype> Size = "Ginormous";

    [DataField, AutoNetworkedField]
    public ProtoId<ItemSizePrototype>? OriginalSize;
}
