using Robust.Shared.GameStates;
using System.Numerics;

namespace Content.Shared.Camera
{
    /// <summary>
    /// Event for zoom changes like overwatch camera eye adjustments.
    /// </summary>
    [ByRefEvent]
    public record struct GetEyeZoomEvent(Vector2 Zoom);
}
