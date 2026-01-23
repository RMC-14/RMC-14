using Content.Shared._RMC14.Deploy;
using Robust.Client.Graphics;

namespace Content.Client._RMC14.Deploy;

/// <summary>
/// Client-side system for handling deploy area overlays and related network events during deployment.
/// </summary>
public sealed class RMCClientDeploySystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<RMCShowDeployAreaEvent>(OnShowDeployArea);
        SubscribeNetworkEvent<RMCHideDeployAreaEvent>(OnHideDeployArea);
    }

    private void OnShowDeployArea(RMCShowDeployAreaEvent ev)
    {
        var overlay = new RMCDeployAreaOverlay();
        overlay.Box = ev.Box;
        overlay.Color = ev.Color;
        _overlayManager.AddOverlay(overlay);
    }

    private void OnHideDeployArea(RMCHideDeployAreaEvent ev)
    {
        _overlayManager.RemoveOverlay<RMCDeployAreaOverlay>();
    }
}
