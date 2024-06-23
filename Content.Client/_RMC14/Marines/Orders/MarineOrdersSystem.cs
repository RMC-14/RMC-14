using Content.Shared._RMC14.Marines.Orders;
using Robust.Client.Graphics;

namespace Content.Client._RMC14.Marines.Orders;

public sealed class MarineOrdersSystem : SharedMarineOrdersSystem
{
    [Dependency] private readonly IOverlayManager _overlays = default!;

    public override void Initialize()
    {
        base.Initialize();

        if (!_overlays.HasOverlay<OrdersOverlay>())
            _overlays.AddOverlay(new OrdersOverlay());
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlays.RemoveOverlay<OrdersOverlay>();
    }
}
