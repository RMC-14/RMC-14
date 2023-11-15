using Content.Shared._CM14.Xenos.Pheromones;
using Robust.Client.Graphics;

namespace Content.Client._CM14.Xenos.Pheromones;

public sealed class XenoPheromonesSystem : SharedXenoPheromonesSystem
{
    [Dependency] private readonly IOverlayManager _overlays = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlays.AddOverlay(new XenoPheromonesOverlay());
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlays.RemoveOverlay<XenoPheromonesOverlay>();
    }
}
