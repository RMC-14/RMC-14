using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Paper;

/// <summary>
/// Makes paper lock editing after the first write, similar to being stamped.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BusinessCardComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool HasBeenWritten;
}
