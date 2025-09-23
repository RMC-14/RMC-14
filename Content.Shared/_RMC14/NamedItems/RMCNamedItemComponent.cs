using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.NamedItems;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedRMCNamedItemSystem))]
public sealed partial class RMCNamedItemComponent : Component
{
    [DataField]
    public EntityUid? User;

    [DataField]
    public RMCNamedItemType? Type;

    [DataField]
    public string Name = string.Empty;
}
