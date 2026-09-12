using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.ERT;

/// <summary>
/// Map marker used as a physical staging pad for loading an ERT shuttle onto the request source map.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RMCERTShuttleStartPadComponent : Component
{
    /// <summary>
    /// Local offset from the marker used when placing a loaded shuttle grid.
    /// </summary>
    [DataField]
    public Vector2 Offset;
}
