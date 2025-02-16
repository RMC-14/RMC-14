using Content.Shared._RMC14.NamedItems;

namespace Content.Server._RMC14.NamedItems;

[RegisterComponent]
[Access(typeof(RMCNamedItemSystem))]
public sealed partial class RMCNameItemOnVendComponent : Component
{
    [DataField(required: true)]
    public RMCNamedItemType Item;

    [DataField]
    public string? Name;
}
