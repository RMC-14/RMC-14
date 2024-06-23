using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Scoping;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedScopeSystem))]
public sealed partial class GunScopingComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Scope;
}
