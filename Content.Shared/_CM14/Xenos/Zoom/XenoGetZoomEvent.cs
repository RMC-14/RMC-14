using System.Numerics;

namespace Content.Shared._CM14.Xenos.Zoom;

[ByRefEvent]
public record struct XenoGetZoomEvent(Vector2 Zoom)
{
    public void Increase(float zoom)
    {
        if (Zoom.X >= zoom)
            return;

        Zoom = new Vector2(zoom, zoom);
    }
}
