using Content.Shared._RMC14.Deploy;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Content.Client._RMC14.Deploy;

/// <summary>
/// Client-side system for handling deploy area overlays and related network events during deployment.
/// </summary>
public sealed class RCMClientlDeploySystem : EntitySystem
{
    private RMCDeployAreaOverlay? _overlay;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<RMCShowDeployAreaEvent>(OnShowDeployArea);
        SubscribeNetworkEvent<RMCHideDeployAreaEvent>(OnHideDeployArea);

        _overlay = new RMCDeployAreaOverlay();
        IoCManager.Resolve<IOverlayManager>().AddOverlay(_overlay);
    }

    private void OnShowDeployArea(RMCShowDeployAreaEvent ev)
    {
        if (_overlay == null) return;
        _overlay.Box = ev.Box;
        _overlay.Color = ev.Color;
        _overlay.Visible = true;
    }

    private void OnHideDeployArea(RMCHideDeployAreaEvent ev)
    {
        if (_overlay == null) return;
        _overlay.Visible = false;
    }
}
