using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Radio;

/// <summary>
///     Text applied before a radio sender's name
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCRadioPrefixComponent : Component
{
    [DataField, AutoNetworkedField]
    public LocId Prefix = string.Empty;
}
