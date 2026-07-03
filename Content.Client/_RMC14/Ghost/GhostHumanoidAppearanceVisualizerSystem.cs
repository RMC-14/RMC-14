using System.Linq;
using Content.Client.DisplacementMap;
using Content.Client.Humanoid;
using Content.Shared._RMC14.Ghost;
using Content.Shared._RMC14.Webbing;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Hands.Components;
using Content.Shared.Humanoid;
using Content.Shared.Item;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Map;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Client._RMC14.Ghost;

public sealed class GhostHumanoidAppearanceVisualizerSystem : EntitySystem
{
    [Dependency] private readonly DisplacementMapSystem _displacement = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private readonly Dictionary<string, CachedClothingData?> _clothingCache = new();
    private readonly Dictionary<string, CachedItemData?> _itemCache = new();

    private static readonly object[] TemplateLayers =
    {
        HumanoidVisualLayers.Chest,
        HumanoidVisualLayers.Head,
        HumanoidVisualLayers.Snout,
        HumanoidVisualLayers.Eyes,
        HumanoidVisualLayers.RArm,
        HumanoidVisualLayers.LArm,
        HumanoidVisualLayers.RLeg,
        HumanoidVisualLayers.LLeg,
        HumanoidVisualLayers.UndergarmentBottom,
        HumanoidVisualLayers.UndergarmentTop,
        "jumpsuit",
        HumanoidVisualLayers.LFoot,
        HumanoidVisualLayers.RFoot,
        HumanoidVisualLayers.LHand,
        HumanoidVisualLayers.RHand,
        HumanoidVisualLayers.Handcuffs,
        "gloves",
        "shoes",
        "id",
        "ears",
        "eyes",
        "belt",
        "outerClothing",
        WebbingVisualLayers.Base,
        "back",
        HumanoidVisualLayers.FacialHair,
        "neck",
        HumanoidVisualLayers.Hair,
        HumanoidVisualLayers.HeadSide,
        HumanoidVisualLayers.HeadTop,
        HumanoidVisualLayers.Tail,
        "mask",
    };

    public override void Initialize()
    {
        SubscribeLocalEvent<GhostHumanoidAppearanceComponent, ComponentStartup>(OnAppearanceStartup);
        SubscribeLocalEvent<GhostHumanoidAppearanceComponent, AfterAutoHandleStateEvent>(OnAppearanceState);
        SubscribeLocalEvent<GhostHumanoidAppearanceComponent, ComponentShutdown>(OnAppearanceShutdown);
    }

    private void OnAppearanceStartup(Entity<GhostHumanoidAppearanceComponent> ent, ref ComponentStartup args)
    {
        RefreshLayers(ent);
    }

    private void OnAppearanceState(Entity<GhostHumanoidAppearanceComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        RefreshLayers(ent);
    }

    private void RefreshLayers(Entity<GhostHumanoidAppearanceComponent> ent)
    {
        if (!TryComp(ent, out SpriteComponent? sprite))
            return;

        var visuals = EnsureComp<GhostHumanoidAppearanceVisualsComponent>(ent);
        EnsureTemplateLayers((ent.Owner, sprite));
        ClearLayers((ent.Owner, sprite, visuals));

        if (sprite.AllLayers.Any())
            sprite[0].Visible = false;

        _humanoid.RefreshAppearance(ent.Owner, ent.Comp.Appearance, sprite);

        var speciesId = (string?)ent.Comp.Appearance.Species;
        var insertionIndices = new Dictionary<string, int>();

        foreach (var clothing in ent.Comp.Clothing)
        {
            AddClothingLayers((ent.Owner, sprite, visuals), clothing, speciesId, insertionIndices);
        }

        foreach (var heldItem in ent.Comp.HeldItems)
        {
            AddHeldItemLayers((ent.Owner, sprite, visuals), heldItem, insertionIndices);
        }

        RaiseLocalEvent(ent, new GhostHumanoidLayersRefreshedEvent());
    }

    private void AddClothingLayers(
        Entity<SpriteComponent, GhostHumanoidAppearanceVisualsComponent> ent,
        GhostClothingSnapshot snapshot,
        string? speciesId,
        Dictionary<string, int> insertionIndices)
    {
        AddMainClothingLayers(ent, snapshot, speciesId, insertionIndices);

        foreach (var accessory in snapshot.Accessories)
        {
            AddAccessoryLayer((ent.Owner, ent.Comp1, ent.Comp2), accessory, insertionIndices);
        }

        if (snapshot.Webbing != null)
            AddWebbingLayer((ent.Owner, ent.Comp1, ent.Comp2), snapshot.Webbing, insertionIndices);
    }

    private void AddMainClothingLayers(
        Entity<SpriteComponent, GhostHumanoidAppearanceVisualsComponent> ent,
        GhostClothingSnapshot snapshot,
        string? speciesId,
        Dictionary<string, int> insertionIndices)
    {
        if (snapshot.PrototypeId == null)
            return;

        var data = GetClothingData(snapshot.PrototypeId);
        if (data == null)
            return;

        var effectiveEquippedPrefix = snapshot.EquippedPrefix ?? data.EquippedPrefix;
        var effectiveEquippedState = snapshot.EquippedState ?? data.EquippedState;

        var rsi = data.Rsi;
        var rsiPath = data.RsiPath;
        if (snapshot.ClothingRsiPath != null)
        {
            rsiPath = snapshot.ClothingRsiPath;
            rsi = null;
            if (_resourceCache.TryGetResource<RSIResource>(SpriteSpecifierSerializer.TextureRoot / rsiPath, out var overrideRes))
                rsi = overrideRes.RSI;
        }

        var resolution = ClothingSystem.ResolveEquippedVisuals(
            data.ClothingVisuals,
            effectiveEquippedPrefix,
            effectiveEquippedState,
            snapshot.Slot,
            speciesId,
            rsiPath,
            state => rsi?.TryGetState(state, out _) == true,
            out var layers);

        if (resolution == ClothingVisualResolution.None || layers == null)
            return;

        var i = 0;
        foreach (var layer in layers)
        {
            var key = layer.MapKeys?.FirstOrDefault() ?? $"{snapshot.Slot}-{i}";
            var renderKey = $"ghosthum-clothing-{snapshot.Slot}-{key}";

            var layerCopy = ClothingSystem.CopyLayer(layer);
            if (string.IsNullOrWhiteSpace(layerCopy.RsiPath))
                layerCopy.RsiPath = rsiPath;
            layerCopy.Offset += snapshot.SlotOffset;

            if (CanApplyLayer(layerCopy, rsi))
            {
                var index = ResolveLayerIndex((ent.Owner, ent.Comp1), snapshot.Slot, renderKey, insertionIndices);
                _sprite.LayerSetData((ent.Owner, ent.Comp1), index, layerCopy);
                ent.Comp2.RenderedLayers.Add(renderKey);
                ent.Comp2.BoostedLayers.Add(renderKey);

                if (snapshot.Displacement != null &&
                    _displacement.TryAddDisplacement(snapshot.Displacement, (ent.Owner, ent.Comp1), index, renderKey, out var dispKey))
                {
                    ent.Comp2.RenderedLayers.Add(dispKey);
                    ent.Comp2.BoostedLayers.Add(dispKey);

                    Entity<SpriteComponent?> spriteEnt = (ent.Owner, ent.Comp1);
                    if (_sprite.LayerMapTryGet(spriteEnt, dispKey, out var dispIdx, false))
                        insertionIndices[snapshot.Slot] = dispIdx;
                }
            }

            i++;
        }
    }

    private void AddAccessoryLayer(
        Entity<SpriteComponent, GhostHumanoidAppearanceVisualsComponent> ent,
        GhostAccessorySnapshot accessory,
        Dictionary<string, int> insertionIndices)
    {
        var renderKey = $"ghosthum-accessory-{accessory.LayerKey}";
        var layer = new PrototypeLayerData
        {
            RsiPath = accessory.Sprite.ToString(),
            State = accessory.State,
            Visible = accessory.Visible,
        };

        if (!CanApplyLayer(layer))
            return;

        var index = ResolveLayerIndex((ent.Owner, ent.Comp1), accessory.BookmarkKey, renderKey, insertionIndices);
        _sprite.LayerSetData((ent.Owner, ent.Comp1), index, layer);
        ent.Comp2.RenderedLayers.Add(renderKey);
        ent.Comp2.BoostedLayers.Add(renderKey);
    }

    private void AddWebbingLayer(
        Entity<SpriteComponent, GhostHumanoidAppearanceVisualsComponent> ent,
        GhostWebbingSnapshot webbing,
        Dictionary<string, int> insertionIndices)
    {
        var layerEnum = webbing.IsOuter ? WebbingVisualLayers.Outer : WebbingVisualLayers.Base;
        var renderKey = $"ghosthum-webbing-{webbing.BookmarkKey}-{layerEnum}";
        var layer = new PrototypeLayerData
        {
            RsiPath = webbing.Sprite.ToString(),
            State = webbing.State,
            Visible = true,
        };

        if (!CanApplyLayer(layer))
            return;

        var index = ResolveLayerIndex((ent.Owner, ent.Comp1), webbing.BookmarkKey, renderKey, insertionIndices);
        _sprite.LayerSetData((ent.Owner, ent.Comp1), index, layer);
        ent.Comp2.RenderedLayers.Add(renderKey);
        ent.Comp2.BoostedLayers.Add(renderKey);
    }

    private void AddHeldItemLayers(
        Entity<SpriteComponent, GhostHumanoidAppearanceVisualsComponent> ent,
        GhostHeldItemSnapshot snapshot,
        Dictionary<string, int> insertionIndices)
    {
        if (snapshot.PrototypeId == null)
            return;

        var data = GetItemData(snapshot.PrototypeId);
        if (data == null)
            return;

        var effectiveHeldPrefix = snapshot.HeldPrefix ?? data.HeldPrefix;

        var rsiPath = snapshot.ItemRsiPath ?? data.RsiPath;

        var locationKey = snapshot.Location.ToString().ToLowerInvariant();
        List<PrototypeLayerData> layers;
        if (data.InhandVisuals.TryGetValue(snapshot.Location, out var explicitLayers))
        {
            layers = explicitLayers;
        }
        else if (rsiPath != null)
        {
            var defaultKey = $"inhand-{locationKey}";
            var state = effectiveHeldPrefix == null ? defaultKey : $"{effectiveHeldPrefix}-{defaultKey}";
            layers = new()
            {
                new PrototypeLayerData
                {
                    RsiPath = rsiPath,
                    State = state,
                    MapKeys = new() { state },
                },
            };
        }
        else
        {
            return;
        }

        var defaultRenderKey = $"inhand-{locationKey}";
        var i = 0;
        foreach (var layer in layers)
        {
            var layerCopy = ClothingSystem.CopyLayer(layer);
            if (string.IsNullOrWhiteSpace(layerCopy.RsiPath))
                layerCopy.RsiPath = rsiPath;

            if (!CanApplyLayer(layerCopy))
            {
                i++;
                continue;
            }

            var key = layerCopy.MapKeys?.FirstOrDefault() ?? (i == 0 ? defaultRenderKey : $"{defaultRenderKey}-{i}");
            layerCopy.MapKeys = null;
            var renderKey = $"ghosthum-inhand-{snapshot.Location}-{key}";

            var index = ResolveLayerIndex((ent.Owner, ent.Comp1), null, renderKey, insertionIndices);
            _sprite.LayerSetData((ent.Owner, ent.Comp1), index, layerCopy);
            ent.Comp2.RenderedLayers.Add(renderKey);
            ent.Comp2.BoostedLayers.Add(renderKey);

            if (snapshot.Displacement != null &&
                _displacement.TryAddDisplacement(snapshot.Displacement, (ent.Owner, ent.Comp1), index, renderKey, out var dispKey))
            {
                ent.Comp2.RenderedLayers.Add(dispKey);
                ent.Comp2.BoostedLayers.Add(dispKey);
            }

            i++;
        }
    }

    private CachedClothingData? GetClothingData(string protoId)
    {
        if (_clothingCache.TryGetValue(protoId, out var cached))
            return cached;

        var dummy = Spawn(protoId, MapCoordinates.Nullspace);
        try
        {
            var clothing = CompOrNull<ClothingComponent>(dummy);
            if (clothing == null)
            {
                _clothingCache[protoId] = null;
                return null;
            }

            RSI? rsi = null;
            if (clothing.RsiPath != null &&
                _resourceCache.TryGetResource<RSIResource>(SpriteSpecifierSerializer.TextureRoot / clothing.RsiPath, out var res))
            {
                rsi = res.RSI;
            }
            else
            {
                rsi = CompOrNull<SpriteComponent>(dummy)?.BaseRSI;
            }

            var data = new CachedClothingData
            {
                RsiPath = rsi?.Path.ToString(),
                Rsi = rsi,
                ClothingVisuals = clothing.ClothingVisuals,
                EquippedPrefix = clothing.EquippedPrefix,
                EquippedState = clothing.EquippedState,
            };

            _clothingCache[protoId] = data;
            return data;
        }
        finally
        {
            Del(dummy);
        }
    }

    private CachedItemData? GetItemData(string protoId)
    {
        if (_itemCache.TryGetValue(protoId, out var cached))
            return cached;

        var dummy = Spawn(protoId, MapCoordinates.Nullspace);
        try
        {
            var item = CompOrNull<ItemComponent>(dummy);
            if (item == null)
            {
                _itemCache[protoId] = null;
                return null;
            }

            RSI? rsi = null;
            if (item.RsiPath != null &&
                _resourceCache.TryGetResource<RSIResource>(SpriteSpecifierSerializer.TextureRoot / item.RsiPath, out var res))
            {
                rsi = res.RSI;
            }
            else
            {
                rsi = CompOrNull<SpriteComponent>(dummy)?.BaseRSI;
            }

            var data = new CachedItemData
            {
                RsiPath = rsi?.Path.ToString(),
                Rsi = rsi,
                InhandVisuals = item.InhandVisuals,
                HeldPrefix = item.HeldPrefix,
            };

            _itemCache[protoId] = data;
            return data;
        }
        finally
        {
            Del(dummy);
        }
    }

    private bool CanApplyLayer(PrototypeLayerData layer, RSI? knownRsi = null)
    {
        if (string.IsNullOrWhiteSpace(layer.State))
            return true;

        var rsi = knownRsi;
        if (rsi == null)
        {
            if (string.IsNullOrWhiteSpace(layer.RsiPath))
                return false;

            if (!_resourceCache.TryGetResource<RSIResource>(SpriteSpecifierSerializer.TextureRoot / layer.RsiPath, out var res))
                return false;

            rsi = res.RSI;
        }

        return rsi.TryGetState(layer.State, out _);
    }

    private int ResolveLayerIndex(
        Entity<SpriteComponent> sprite,
        string? bookmarkKey,
        string renderKey,
        Dictionary<string, int> insertionIndices)
    {
        Entity<SpriteComponent?> spriteEnt = (sprite.Owner, sprite.Comp);
        if (bookmarkKey == null ||
            !_sprite.LayerMapTryGet(spriteEnt, bookmarkKey, out var bookmarkIndex, false))
        {
            return _sprite.LayerMapReserve(spriteEnt, renderKey);
        }

        var index = insertionIndices.TryGetValue(bookmarkKey, out var currentIndex)
            ? currentIndex + 1
            : bookmarkIndex + 1;

        _sprite.AddBlankLayer(sprite, index);
        _sprite.LayerMapRemove(spriteEnt, renderKey);
        _sprite.LayerMapSet(spriteEnt, renderKey, index);
        insertionIndices[bookmarkKey] = index;
        return index;
    }

    private void EnsureTemplateLayers(Entity<SpriteComponent> ent)
    {
        Entity<SpriteComponent?> spriteEnt = (ent.Owner, ent.Comp);
        foreach (var key in TemplateLayers)
        {
            var index = key switch
            {
                Enum enumKey => _sprite.LayerMapReserve(spriteEnt, enumKey),
                string stringKey => _sprite.LayerMapReserve(spriteEnt, stringKey),
                _ => throw new ArgumentOutOfRangeException(nameof(key)),
            };

            ent.Comp[index].Visible = false;
        }
    }

    private void OnAppearanceShutdown(Entity<GhostHumanoidAppearanceComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp(ent, out SpriteComponent? sprite) ||
            !TryComp(ent, out GhostHumanoidAppearanceVisualsComponent? visuals))
        {
            return;
        }

        ClearLayers((ent.Owner, sprite, visuals));

        if (sprite.AllLayers.Any())
            sprite[0].Visible = true;

        RemCompDeferred<GhostHumanoidAppearanceVisualsComponent>(ent);
    }

    private void ClearLayers(Entity<SpriteComponent, GhostHumanoidAppearanceVisualsComponent> ent)
    {
        foreach (var key in ent.Comp2.RenderedLayers)
        {
            _sprite.RemoveLayer((ent.Owner, ent.Comp1), key, false);
        }

        ent.Comp2.RenderedLayers.Clear();
        ent.Comp2.BoostedLayers.Clear();
    }

    private sealed class CachedClothingData
    {
        public string? RsiPath;
        public RSI? Rsi;
        public Dictionary<string, List<PrototypeLayerData>> ClothingVisuals = new();
        public string? EquippedPrefix;
        public string? EquippedState;
    }

    private sealed class CachedItemData
    {
        public string? RsiPath;
        public RSI? Rsi;
        public Dictionary<HandLocation, List<PrototypeLayerData>> InhandVisuals = new();
        public string? HeldPrefix;
    }
}
