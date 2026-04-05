using Content.Shared._RMC14.Xenonids.Eye;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.Ghost;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client._RMC14.Xenonids.Eye;

public sealed class QueenEyeVisibilitySystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<QueenEyeComponent, ComponentStartup>(OnQueenEyeRefresh);
        SubscribeLocalEvent<QueenEyeComponent, AfterAutoHandleStateEvent>(OnQueenEyeRefresh);
        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnLocalPlayerChanged);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnLocalPlayerChanged);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<QueenEyeComponent, SpriteComponent>();

        while (query.MoveNext(out var uid, out var queenEye, out var sprite))
        {
            Refresh(uid, queenEye, sprite);
        }
    }

    private void OnQueenEyeRefresh(Entity<QueenEyeComponent> ent, ref ComponentStartup args)
    {
        if (TryComp<SpriteComponent>(ent, out var sprite))
            Refresh(ent.Owner, ent.Comp, sprite);
    }

    private void OnQueenEyeRefresh(Entity<QueenEyeComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (TryComp<SpriteComponent>(ent, out var sprite))
            Refresh(ent.Owner, ent.Comp, sprite);
    }

    private void OnLocalPlayerChanged(LocalPlayerAttachedEvent args)
    {
        RefreshAll();
    }

    private void OnLocalPlayerChanged(LocalPlayerDetachedEvent args)
    {
        RefreshAll();
    }

    private void RefreshAll()
    {
        var query = EntityQueryEnumerator<QueenEyeComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var queenEye, out var sprite))
        {
            Refresh(uid, queenEye, sprite);
        }
    }

    private void Refresh(EntityUid uid, QueenEyeComponent queenEye, SpriteComponent sprite)
    {
        var local = _player.LocalEntity;
        var isGhost = local != null && HasComp<GhostComponent>(local.Value);
        var isQueen = local != null && queenEye.Queen == local.Value;
        var sameHive = local != null && SameHive(local.Value, uid);
        var visible = local != null &&
                      (isGhost ||
                       isQueen ||
                       sameHive);

        if (sprite.Visible == visible)
            return;

        _sprite.SetVisible((uid, sprite), visible);
    }

    private bool SameHive(EntityUid a, EntityUid b)
    {
        return TryComp<HiveMemberComponent>(a, out var aHive) &&
               TryComp<HiveMemberComponent>(b, out var bHive) &&
               aHive.Hive != null &&
               aHive.Hive == bHive.Hive;
    }
}
