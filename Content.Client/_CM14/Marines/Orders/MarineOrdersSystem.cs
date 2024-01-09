using Content.Shared._CM14.Marines.Orders;
using Robust.Client.Graphics;

namespace Content.Client._CM14.Marines.Orders;

public sealed class MarineOrdersSystem : SharedMarineOrdersSystem
{

    [Dependency] private readonly IOverlayManager _overlays = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlays.AddOverlay(new OrdersOverlay());
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlays.RemoveOverlay<OrdersOverlay>();
    }
}
