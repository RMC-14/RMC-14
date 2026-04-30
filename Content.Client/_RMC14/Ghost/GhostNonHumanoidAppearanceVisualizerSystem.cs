using System.Collections.Generic;
using System.Linq;
using Content.Shared._RMC14.Ghost;
using Content.Shared._RMC14.Xenonids.Damage;
using Robust.Client.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Ghost;

public sealed class GhostNonHumanoidAppearanceVisualizerSystem : EntitySystem
{
    private const string BaseLayerKey = "ghost-non-humanoid-base";

    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private readonly Dictionary<string, PrototypeSpriteData?> _prototypeSprites = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<GhostNonHumanoidAppearanceComponent, ComponentStartup>(OnAppearanceStartup);
        SubscribeLocalEvent<GhostNonHumanoidAppearanceComponent, AfterAutoHandleStateEvent>(OnAppearanceState);
    }

    private void OnAppearanceStartup(Entity<GhostNonHumanoidAppearanceComponent> ent, ref ComponentStartup args)
    {
        Refresh(ent);
    }

    private void OnAppearanceState(Entity<GhostNonHumanoidAppearanceComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        Refresh(ent);
    }

    private void Refresh(Entity<GhostNonHumanoidAppearanceComponent> ent)
    {
        if (!TryComp(ent, out SpriteComponent? sprite))
            return;

        if (!TryResolveSprite(ent.Comp, out var baseSprite, out var state))
            return;

        if (sprite.AllLayers.Any())
            sprite[0].Visible = false;

        var baseLayer = _sprite.LayerMapReserve((ent.Owner, sprite), BaseLayerKey);
        _sprite.LayerSetRsi((ent.Owner, sprite), baseLayer, baseSprite);
        _sprite.LayerSetVisible((ent.Owner, sprite), baseLayer, true);

        if (ent.Comp.SpentParasite && HasState((ent.Owner, sprite), baseLayer, "impregnated"))
        {
            _sprite.LayerSetRsiState((ent.Owner, sprite), baseLayer, "impregnated");
        }
        else if (state != null && HasState((ent.Owner, sprite), baseLayer, state))
        {
            _sprite.LayerSetRsiState((ent.Owner, sprite), baseLayer, state);
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
        GhostNonHumanoidAppearanceComponent appearance,
        out ResPath baseSprite,
        out string? state)
    {
        if (appearance.Sprite is { } directSprite)
        {
            baseSprite = directSprite;
            state = appearance.State;
            return true;
        }

        if (appearance.SourcePrototype is not { } prototypeId)
        {
            baseSprite = default;
            state = default;
            return false;
        }

        if (!_prototypeSprites.TryGetValue(prototypeId, out var cached) &&
            !TryCachePrototypeSprite(prototypeId, out cached))
        {
            baseSprite = default;
            state = default;
            return false;
        }

        if (cached is not { BaseSprite: { } resolvedBase })
        {
            baseSprite = default;
            state = default;
            return false;
        }

        baseSprite = resolvedBase;
        state = cached.State;
        return true;
    }

    private bool TryCachePrototypeSprite(string prototypeId, out PrototypeSpriteData? cached)
    {
        cached = default;

        if (!_prototype.TryIndex<EntityPrototype>(prototypeId, out var prototype))
            return false;

        var dummy = Spawn(prototype.ID, MapCoordinates.Nullspace);
        try
        {
            if (!TryComp(dummy, out SpriteComponent? sprite))
                return false;

            cached = ResolveVisibleSprite(sprite);
            _prototypeSprites[prototypeId] = cached;
            return true;
        }
        finally
        {
            Del(dummy);
        }
    }

    private PrototypeSpriteData? ResolveVisibleSprite(SpriteComponent sprite)
    {
        foreach (var layer in sprite.AllLayers.Where(layer => layer.Visible))
        {
            var rsi = layer.ActualRsi?.Path;
            var state = layer.RsiState.Name;

            if (rsi == null || state == null)
                continue;

            return new PrototypeSpriteData(rsi.Value, state);
        }

        if (sprite.BaseRSI?.Path is { } baseRsi)
            return new PrototypeSpriteData(baseRsi, null);

        return null;
    }

    private bool HasState(Entity<SpriteComponent> ent, int layer, string state)
    {
        Entity<SpriteComponent?> spriteEnt = (ent.Owner, ent.Comp);
        return _sprite.LayerGetEffectiveRsi(spriteEnt, layer)?.TryGetState(state, out _) == true;
    }

    private sealed record PrototypeSpriteData(ResPath BaseSprite, string? State);
}
