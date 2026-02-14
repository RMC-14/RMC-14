using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Actions;

// Placed on an action entity that was swapped from instant to world-target mode.
// Stores original state so it can be restored, preserving action bar position.
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SwappableActionSystem))]
public sealed partial class SwappableActionComponent : Component
{
    [DataField, AutoNetworkedField]
    public string OriginalName = string.Empty;

    [DataField, AutoNetworkedField]
    public string OriginalDescription = string.Empty;
}
