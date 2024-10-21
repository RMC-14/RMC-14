using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Scoping;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedScopeSystem))]
public sealed partial class ScopingComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public EntityUid? Scope;

    [ViewVariables, AutoNetworkedField]
    public Vector2 EyeOffset;

    [ViewVariables, AutoNetworkedField]
    public bool AllowMovement;

    // To account for other zoom-altering items, such as the SGO sights
    [ViewVariables, AutoNetworkedField]
    public Vector2 PreviousZoom = new Vector2(1.0f, 1.0f);
}
