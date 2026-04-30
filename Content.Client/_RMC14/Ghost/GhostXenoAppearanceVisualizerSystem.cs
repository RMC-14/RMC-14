using System.Collections.Generic;
using System.Linq;
using Content.Shared._RMC14.Ghost;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Damage;
using Robust.Client.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Ghost;

public sealed class GhostXenoAppearanceVisualizerSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private readonly Dictionary<string, ResPath?> _prototypeSprites = new();

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

        if (!TryResolveSprite(ent.Comp, out var baseSprite))
            return;

        if (sprite.AllLayers.Any())
            sprite[0].Visible = false;

        var baseLayer = _sprite.LayerMapReserve((ent.Owner, sprite), XenoVisualLayers.Base);
        _sprite.LayerSetRsi((ent.Owner, sprite), baseLayer, baseSprite);
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

        var damageLayer = _sprite.LayerMapReserve((ent.Owner, sprite), RMCDamageVisualLayers.Base);
        _sprite.LayerSetVisible((ent.Owner, sprite), damageLayer, false);
    }

    private bool TryResolveSprite(
        GhostXenoAppearanceComponent appearance,
        out ResPath baseSprite)
    {
        if (appearance.Sprite is { } directSprite)
        {
            baseSprite = directSprite;
            return true;
        }

        if (appearance.SourcePrototype is not { } prototypeId)
        {
            baseSprite = default;
            return false;
        }

        if (!_prototypeSprites.TryGetValue(prototypeId, out var cached) &&
            !TryCachePrototypeSprite(prototypeId, out cached))
        {
            baseSprite = default;
            return false;
        }

        if (cached is not { } resolvedBase)
        {
            baseSprite = default;
            return false;
        }

        baseSprite = resolvedBase;
        return true;
    }

    private bool TryCachePrototypeSprite(string prototypeId, out ResPath? cached)
    {
        cached = default;

        if (!_prototype.TryIndex<EntityPrototype>(prototypeId, out var prototype))
            return false;

        var dummy = Spawn(prototype.ID, MapCoordinates.Nullspace);
        try
        {
            if (!TryComp(dummy, out SpriteComponent? sprite))
                return false;

            cached = sprite.BaseRSI?.Path;
            _prototypeSprites[prototypeId] = cached;
            return true;
        }
        finally
        {
            Del(dummy);
        }
    }

    private bool HasState(Entity<SpriteComponent> ent, int layer, string state)
    {
        Entity<SpriteComponent?> spriteEnt = (ent.Owner, ent.Comp);
        return _sprite.LayerGetEffectiveRsi(spriteEnt, layer)?.TryGetState(state, out _) == true;
    }
}
