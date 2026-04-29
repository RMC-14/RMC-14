using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Server._RMC14.Dropship;

/// <summary>
/// Lets a destination marker stay at a mapper-facing position while FTL uses a different exact grid coordinate.
/// </summary>
[RegisterComponent]
public sealed partial class RMCDropshipDestinationCoordinatesOverrideComponent : Component
{
    [ViewVariables]
    public MapCoordinates Coordinates;

    [ViewVariables]
    public Angle Rotation;
}
