using Content.Shared._RMC14.NamedItems;
using Robust.Shared.GameStates;

namespace Content.Server._RMC14.NamedItems;

[RegisterComponent]
[Access(typeof(RMCNamedItemSystem))]
public sealed partial class RMCNameItemOnVendComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public RMCNamedItemType Item;

    [DataField, AutoNetworkedField]
    public string? Name;
}
