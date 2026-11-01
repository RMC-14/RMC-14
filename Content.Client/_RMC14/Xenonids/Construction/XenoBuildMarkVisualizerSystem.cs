using Content.Shared._RMC14.Xenonids.Construction.BuildMark;
using Robust.Client.GameObjects;
using Robust.Client.Player;

namespace Content.Client._RMC14.Xenonids.Construction;

public sealed class XenoBuildMarkVisualizerSystem : EntitySystem
{
    [Dependency] private readonly XenoBuildMarkSystem _buildMark = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoBuildMarkComponent, ComponentStartup>(OnMarkStartup);
        SubscribeLocalEvent<XenoBuildMarkComponent, AfterAutoHandleStateEvent>(OnMarkState);
    }

    private void OnMarkStartup(Entity<XenoBuildMarkComponent> ent, ref ComponentStartup args)
    {
        UpdateAppearance(ent);
    }

    private void OnMarkState(Entity<XenoBuildMarkComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateAppearance(ent);
    }

    private void UpdateAppearance(Entity<XenoBuildMarkComponent> ent)
    {
        if (!TryComp(ent, out SpriteComponent? sprite))
            return;

        _sprite.LayerSetRsiState((ent, sprite), 0, _buildMark.GetMarkState(ent.Comp));
        _sprite.SetColor((ent, sprite), ent.Comp.Queued ? ent.Comp.QueuedColor : Color.White);
    }

    public override void Update(float frameTime)
    {
        var local = _player.LocalEntity;
        var query = EntityQueryEnumerator<XenoBuildMarkComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var mark, out var sprite))
        {
            var visible = local != null && mark.Planter == local;
            if (sprite.Visible == visible)
                continue;

            if (visible)
                UpdateAppearance((uid, mark));

            _sprite.SetVisible((uid, sprite), visible);
        }
    }
}
