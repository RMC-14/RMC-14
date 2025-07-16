using Content.Shared._RMC14.Deploy;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Content.Client._RMC14.Deploy;

public sealed class DeployAreaSystem : EntitySystem
{
    private DeployAreaOverlay? _overlay;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<ShowDeployAreaEvent>(OnShowDeployArea);
        SubscribeNetworkEvent<HideDeployAreaEvent>(OnHideDeployArea);

        _overlay = new DeployAreaOverlay();
        IoCManager.Resolve<IOverlayManager>().AddOverlay(_overlay);
    }

    private void OnShowDeployArea(ShowDeployAreaEvent ev)
    {
        if (_overlay == null) return;
        _overlay.Center = ev.Center;
        _overlay.Width = ev.Width;
        _overlay.Height = ev.Height;
        _overlay.Color = ev.Color;
        _overlay.Visible = true;
    }

    private void OnHideDeployArea(HideDeployAreaEvent ev)
    {
        if (_overlay == null) return;
        _overlay.Visible = false;
    }
}
