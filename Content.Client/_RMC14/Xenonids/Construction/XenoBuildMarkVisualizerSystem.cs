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
        TryUpdateAppearance(ent);
    }

    private void OnMarkState(Entity<XenoBuildMarkComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        TryUpdateAppearance(ent);
    }

    private void OnMarkUpdated(Entity<XenoBuildMarkComponent> ent, ref XenoBuildMarkUpdatedEvent args)
    {
        TryUpdateAppearance(ent);
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
        var query = AllEntityQuery<XenoBuildMarkComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var mark, out var sprite))
        {
            UpdateAppearance((uid, mark, sprite));
        }
    }

    private void TryUpdateAppearance(Entity<XenoBuildMarkComponent> ent)
    {
        if (TryComp(ent, out SpriteComponent? sprite))
            UpdateAppearance((ent, ent.Comp, sprite));
    }

    private void UpdateAppearance(Entity<XenoBuildMarkComponent, SpriteComponent> ent)
    {
        var spriteEnt = (ent.Owner, ent.Comp2);
        var local = _player.LocalEntity;
        _sprite.SetVisible(spriteEnt, local != null && ent.Comp1.Planter == local);
        _sprite.SetColor(spriteEnt, ent.Comp1.Queued ? ent.Comp1.QueuedColor : Color.White);

        if (_sprite.LayerMapTryGet(spriteEnt, XenoBuildMarkLayers.Base, out var layer, false))
            _sprite.LayerSetRsiState(spriteEnt, layer, _buildMark.GetMarkState(ent.Comp1));
    }
}
