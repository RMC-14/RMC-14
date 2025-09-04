using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Marines;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class JobPrefixComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public LocId Prefix = string.Empty;

    /// <summary>
    /// Additional prefix that will be appended to the main prefix
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId? AdditionalPrefix;
}
