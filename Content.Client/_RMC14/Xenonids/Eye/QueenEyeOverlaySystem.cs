using Content.Shared._RMC14.Xenonids.Eye;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client._RMC14.Xenonids.Eye;

public sealed class QueenEyeOverlaySystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<QueenEyeActionComponent, AfterAutoHandleStateEvent>(OnUpdated);
        SubscribeLocalEvent<QueenEyeActionComponent, QueenEyeActionUpdated>(OnUpdated);
        SubscribeLocalEvent<QueenEyeActionComponent, LocalPlayerAttachedEvent>(OnAttached);
        SubscribeLocalEvent<QueenEyeActionComponent, LocalPlayerDetachedEvent>(OnDetached);
    }

    private void OnUpdated<T>(Entity<QueenEyeActionComponent> ent, ref T args)
    {
        Updated(ent);
    }

    private void OnAttached(Entity<QueenEyeActionComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        Updated(ent);
    }

    private void OnDetached(Entity<QueenEyeActionComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        _overlay.RemoveOverlay<QueenEyeOverlay>();
    }

    private void Updated(Entity<QueenEyeActionComponent> ent)
    {
        if (_player.LocalEntity != ent)
            return;

        if (ent.Comp.Eye == null)
        {
            _overlay.RemoveOverlay<QueenEyeOverlay>();
            return;
        }

        if (!_overlay.HasOverlay<QueenEyeOverlay>())
            _overlay.AddOverlay(new QueenEyeOverlay());
    }
}
