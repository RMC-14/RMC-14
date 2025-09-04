using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.NamedItems;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedRMCNamedItemSystem))]
public sealed partial class RMCNameItemOnVendComponent : Component
{
    [DataField(required: true)]
    public RMCNamedItemType Item;
}
