using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Marines.Dogtags;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TakeableTagsComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool TagsTaken = false;
}
