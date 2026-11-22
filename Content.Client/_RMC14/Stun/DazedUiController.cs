using Content.Shared._RMC14.Stun;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Stun;

public sealed class DazedUiController : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private DazedOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new DazedOverlay(_entityManager, _playerManager, _prototypeManager);

        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnPlayerAttach);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnPlayerDetached);

        SubscribeLocalEvent<RMCDazedComponent, ComponentStartup>(OnStartup);
    }

    private void OnPlayerAttach(LocalPlayerAttachedEvent args)
    {
        if (!_overlayManager.HasOverlay<DazedOverlay>())
            _overlayManager.AddOverlay(_overlay);
    }

    private void OnPlayerDetached(LocalPlayerDetachedEvent args)
    {
        if (_overlayManager.HasOverlay<DazedOverlay>())
             _overlayManager.RemoveOverlay(_overlay);
    }

    private void OnStartup(Entity<RMCDazedComponent> ent, ref ComponentStartup args)
    {
        if (ent == _playerManager.LocalEntity)
        {
            if (!_overlayManager.HasOverlay<DazedOverlay>())
                _overlayManager.AddOverlay(_overlay);
        }
    }
}
