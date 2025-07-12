using Content.Shared._RMC14.Stun;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client._RMC14.Stun;

public sealed class DazedUiController : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    private DazedOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new DazedOverlay();

        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnPlayerAttach);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnPlayerDetached);

        SubscribeLocalEvent<RMCDazedComponent, ComponentStartup>(OnStartup);
        SubscribeNetworkEvent<DazedComponentShutdownEvent>(OnLocalPlayerDazedShutdown);
    }

    private void OnPlayerAttach(LocalPlayerAttachedEvent args)
    {
        _overlay.IsEnabled = _entityManager.HasComponent<RMCDazedComponent>(args.Entity);
        if (!_overlayManager.HasOverlay<DazedOverlay>())
            _overlayManager.AddOverlay(_overlay);
    }

    private void OnPlayerDetached(LocalPlayerDetachedEvent args)
    {
        if (_overlayManager.HasOverlay<DazedOverlay>())
             _overlayManager.RemoveOverlay(_overlay);
        _overlay.IsEnabled = false;
    }

    private void OnStartup(Entity<RMCDazedComponent> ent, ref ComponentStartup args)
    {
        if (ent == _playerManager.LocalEntity)
        {
            _overlay.IsEnabled = true;
            if (!_overlayManager.HasOverlay<DazedOverlay>())
                _overlayManager.AddOverlay(_overlay);
        }
    }

    private void OnLocalPlayerDazedShutdown(DazedComponentShutdownEvent args)
    {
         _overlay.IsEnabled = false;
    }
}
