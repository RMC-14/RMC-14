using Content.Shared._RMC14.Telephone;
using Robust.Client.Graphics;

namespace Content.Client._RMC14.Telephone;

public sealed class TelephoneSystem : SharedTelephoneSystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();
        if (!_overlay.HasOverlay<TelephoneOverlay>())
            _overlay.AddOverlay(new TelephoneOverlay());
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlay.RemoveOverlay<TelephoneOverlay>();
    }
}
