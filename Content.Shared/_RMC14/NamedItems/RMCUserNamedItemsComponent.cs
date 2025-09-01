using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.NamedItems;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedRMCNamedItemSystem))]
public sealed partial class RMCUserNamedItemsComponent : Component
{
    [DataField]
    public SharedRMCNamedItems Names = new();

    [DataField]
    public EntityUid?[] Entities = new EntityUid?[SharedRMCNamedItemSystem.TypeCount];
}
