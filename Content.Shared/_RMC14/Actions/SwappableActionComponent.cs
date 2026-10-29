using Content.Shared.Actions;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Actions;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SwappableActionSystem))]
public sealed partial class SwappableActionComponent : Component
{
    [DataField, AutoNetworkedField]
    public string OriginalName = string.Empty;

    [DataField, AutoNetworkedField]
    public string OriginalDescription = string.Empty;

    [DataField, AutoNetworkedField]
    public bool IsSwapped;

    [DataField, NonSerialized]
    public BaseActionEvent? SwappedEventTemplate;

    [DataField, NonSerialized]
    public BaseActionEvent? OriginalEventTemplate;
}
