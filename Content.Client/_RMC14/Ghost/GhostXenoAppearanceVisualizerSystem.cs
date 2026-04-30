using System.Linq;
using Content.Shared._RMC14.Ghost;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Damage;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Ghost;

public sealed class GhostXenoAppearanceVisualizerSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GhostXenoAppearanceComponent, ComponentStartup>(OnAppearanceStartup);
        SubscribeLocalEvent<GhostXenoAppearanceComponent, AfterAutoHandleStateEvent>(OnAppearanceState);
    }

    private void OnAppearanceStartup(Entity<GhostXenoAppearanceComponent> ent, ref ComponentStartup args)
    {
        Refresh(ent);
    }

    private void OnAppearanceState(Entity<GhostXenoAppearanceComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        Refresh(ent);
    }

    private void Refresh(Entity<GhostXenoAppearanceComponent> ent)
    {
        if (!TryComp(ent, out SpriteComponent? sprite))
            return;

        if (sprite.AllLayers.Any())
            sprite[0].Visible = false;

        var baseLayer = _sprite.LayerMapReserve((ent.Owner, sprite), XenoVisualLayers.Base);
        _sprite.LayerSetRsi((ent.Owner, sprite), baseLayer, ent.Comp.Sprite);
        _sprite.LayerSetVisible((ent.Owner, sprite), baseLayer, true);

        if (ent.Comp.SpentParasite)
        {
            _sprite.LayerSetRsiState((ent.Owner, sprite), baseLayer, "impregnated");
        }
        else if (HasState((ent.Owner, sprite), baseLayer, "alive"))
        {
            _sprite.LayerSetRsiState((ent.Owner, sprite), baseLayer, "alive");
        }
        else if (HasState((ent.Owner, sprite), baseLayer, "dead"))
        {
            _sprite.LayerSetRsiState((ent.Owner, sprite), baseLayer, "dead");
        }

        var oviLayer = _sprite.LayerMapReserve((ent.Owner, sprite), XenoVisualLayers.Ovipositor);
        if (ent.Comp.OvipositorSprite != null && ent.Comp.OvipositorState != null)
        {
            _sprite.LayerSetRsi((ent.Owner, sprite), oviLayer, ent.Comp.OvipositorSprite.Value, ent.Comp.OvipositorState);
            _sprite.LayerSetVisible((ent.Owner, sprite), oviLayer, true);
            _sprite.LayerSetVisible((ent.Owner, sprite), baseLayer, false);
        }
        else
        {
            _sprite.LayerSetVisible((ent.Owner, sprite), oviLayer, false);
        }

        var damageLayer = _sprite.LayerMapReserve((ent.Owner, sprite), RMCDamageVisualLayers.Base);
        _sprite.LayerSetVisible((ent.Owner, sprite), damageLayer, false);
    }

    private bool HasState(Entity<SpriteComponent> ent, int layer, string state)
    {
        Entity<SpriteComponent?> spriteEnt = (ent.Owner, ent.Comp);
        return _sprite.LayerGetEffectiveRsi(spriteEnt, layer)?.TryGetState(state, out _) == true;
    }
}
