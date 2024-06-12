using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Scoping;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ScopeUserComponent : Component
{
    /// <summary>
    /// The entity that's being used to scope in
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? ScopingItem;
}
