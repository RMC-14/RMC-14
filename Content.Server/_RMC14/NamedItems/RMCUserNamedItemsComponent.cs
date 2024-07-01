using Content.Shared._RMC14.NamedItems;
using Robust.Shared.GameStates;

namespace Content.Server._RMC14.NamedItems;

[RegisterComponent]
[Access(typeof(RMCNamedItemSystem))]
public sealed partial class RMCUserNamedItemsComponent : Component
{
    [DataField]
    public SharedRMCNamedItems Names = new();
}
