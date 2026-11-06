using Content.Shared._RMC14.Xenonids.Construction.BuildMark;
using Content.Shared._RMC14.Xenonids.Construction.Events;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Player;

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
        SubscribeLocalEvent<XenoBuildMarkComponent, XenoBuildMarkUpdatedEvent>(OnMarkUpdated);

        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnLocalPlayerAttached);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnLocalPlayerDetached);
    }

    private void OnMarkStartup(Entity<XenoBuildMarkComponent> ent, ref ComponentStartup args)
    {
        UpdateAppearance(ent);
    }

    private void OnMarkState(Entity<XenoBuildMarkComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateAppearance(ent);
    }

    private void OnMarkUpdated(Entity<XenoBuildMarkComponent> ent, ref XenoBuildMarkUpdatedEvent args)
    {
        UpdateAppearance(ent);
    }

    private void OnLocalPlayerAttached(LocalPlayerAttachedEvent args)
    {
        UpdateAllAppearances();
    }

    private void OnLocalPlayerDetached(LocalPlayerDetachedEvent args)
    {
        UpdateAllAppearances();
    }

    private void UpdateAllAppearances()
    {
        var query = EntityQueryEnumerator<XenoBuildMarkComponent>();
        while (query.MoveNext(out var uid, out var mark))
        {
            UpdateAppearance((uid, mark));
        }
    }

    private void UpdateAppearance(Entity<XenoBuildMarkComponent> ent)
    {
        if (!TryComp(ent, out SpriteComponent? sprite))
            return;

        var local = _player.LocalEntity;
        _sprite.SetVisible((ent, sprite), local != null && ent.Comp.Planter == local);
        _sprite.LayerSetRsiState((ent, sprite), 0, _buildMark.GetMarkState(ent.Comp));
        _sprite.SetColor((ent, sprite), ent.Comp.Queued ? ent.Comp.QueuedColor : Color.White);
    }
}
