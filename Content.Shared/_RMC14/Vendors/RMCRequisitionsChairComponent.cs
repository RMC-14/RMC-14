using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Vendors;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCRequisitionsChairComponent : Component
{
    /// <summary>
    /// The Offset that the item will be placed at from the marker.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Vector2 OffsetItem { get; set; }
}
