using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Marines;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class JobPrefixComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public string Prefix = string.Empty;
}
