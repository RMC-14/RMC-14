using System.Linq;
using System.Numerics;
using System.Text.Json;
using Content.Shared._RMC14.Ghost;
using Content.Shared._RMC14.Humanoid;
using Content.Shared._RMC14.UniformAccessories;
using Content.Shared._RMC14.Webbing;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.DisplacementMap;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Robust.Shared.Containers;
using Robust.Shared.ContentPack;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Utility;

namespace Content.Server.Ghost;

public sealed partial class GhostSystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly IResourceManager _resources = default!;

    private readonly Dictionary<ResPath, HashSet<string>?> _rsiStateCache = new();

    private void CopyDeathAppearance(EntityUid source, EntityUid ghost)
    {
        if (!TryComp(source, out HumanoidAppearanceComponent? sourceHumanoid))
        {
            TryCopyNonHumanoidDeathAppearance(source, ghost);
            return;
        }

        var ghostAppearance = EnsureComp<GhostHumanoidAppearanceComponent>(ghost);
        ghostAppearance.Appearance = SnapshotHumanoidAppearance(sourceHumanoid);
        ghostAppearance.Layers.Clear();

        if (TryComp(source, out InventoryComponent? sourceInventory))
            AppendGhostInventoryLayers(source, sourceInventory, sourceHumanoid, ghostAppearance);

        if (TryComp(source, out HandsComponent? sourceHands))
            AppendGhostHandLayers(source, sourceHands, ghostAppearance);

        Dirty(ghost, ghostAppearance);
    }

    private void AppendGhostInventoryLayers(
        EntityUid source,
        InventoryComponent sourceInventory,
        HumanoidAppearanceComponent sourceHumanoid,
        GhostHumanoidAppearanceComponent ghostAppearance)
    {
        var slotEnumerator = _inventory.GetSlotEnumerator((source, sourceInventory));
        while (slotEnumerator.NextItem(out var item, out var slot))
        {
            if (!TryComp(item, out ClothingComponent? clothing))
                continue;

            AddEquipmentVisuals(
                source,
                item,
                slot.Name,
                sourceInventory.SpeciesId,
                clothing,
                GetClothingDisplacement(sourceInventory, sourceHumanoid, slot.Name),
                slot.Offset,
                ghostAppearance);
        }
    }

    private void AppendGhostHandLayers(EntityUid source, HandsComponent sourceHands, GhostHumanoidAppearanceComponent ghostAppearance)
    {
        foreach (var handName in sourceHands.SortedHands)
        {
            if (!_hands.TryGetHeldItem((source, sourceHands), handName, out var held) ||
                !TryComp(held.Value, out ItemComponent? item) ||
                !_hands.TryGetHand((source, sourceHands), handName, out var hand))
            {
                continue;
            }

            AddInhandVisuals(held.Value, hand.Value.Location, item, GetHandDisplacement(sourceHands, hand.Value.Location), ghostAppearance);
        }
    }

    private void AddEquipmentVisuals(
        EntityUid wearer,
        EntityUid equipment,
        string slot,
        string? speciesId,
        ClothingComponent clothing,
        DisplacementData? displacement,
        Vector2 slotOffset,
        GhostHumanoidAppearanceComponent ghostAppearance)
    {
        if (!TryGetClothingVisuals(wearer, equipment, clothing, slot, speciesId, out var layers))
            return;

        var clothingRsiPath = GetClothingRsiPath(equipment, clothing);
        for (var i = 0; i < layers.Count; i++)
        {
            var layer = CopyLayer(layers[i]);
            PopulateFallbackRsiPath(layer, clothingRsiPath);
            layer.Offset += slotOffset;

            var key = layer.MapKeys?.FirstOrDefault() ?? $"{slot}-{i}";
            layer.MapKeys = null;
            AddSnapshot(key, layer, displacement, slot, true, ghostAppearance);
        }
    }

    private void AddInhandVisuals(
        EntityUid itemUid,
        HandLocation location,
        ItemComponent item,
        DisplacementData? displacement,
        GhostHumanoidAppearanceComponent ghostAppearance)
    {
        var itemRsiPath = GetItemRsiPath(itemUid, item);
        if (!TryGetInhandVisuals(item, location, itemRsiPath, out var layers))
            return;

        var defaultKey = $"inhand-{location.ToString().ToLowerInvariant()}";
        for (var i = 0; i < layers.Count; i++)
        {
            var layer = CopyLayer(layers[i]);
            PopulateFallbackRsiPath(layer, itemRsiPath);

            var key = layer.MapKeys?.FirstOrDefault() ?? (i == 0 ? defaultKey : $"{defaultKey}-{i}");
            layer.MapKeys = null;
            AddSnapshot(key, layer, displacement, null, true, ghostAppearance);
        }
    }

    private bool TryGetClothingVisuals(
        EntityUid wearer,
        EntityUid clothingUid,
        ClothingComponent clothing,
        string slot,
        string? speciesId,
        out List<PrototypeLayerData> layers)
    {
        layers = new();

        var clothingRsiPath = GetClothingRsiPath(clothingUid, clothing);
        var resolution = ClothingSystem.ResolveEquippedVisuals(
            clothing,
            slot,
            speciesId,
            clothingRsiPath,
            state => RsiHasState(clothingRsiPath, state),
            out var resolvedLayers);

        switch (resolution)
        {
            case ClothingVisualResolution.Species:
            case ClothingVisualResolution.Explicit:
                if (resolvedLayers == null)
                    return TryAppendAdditionalClothingVisuals(wearer, clothingUid, slot, layers);
                layers.AddRange(resolvedLayers.Select(CopyLayer));
                break;
            case ClothingVisualResolution.Default:
                if (resolvedLayers == null)
                    return TryAppendAdditionalClothingVisuals(wearer, clothingUid, slot, layers);
                layers.AddRange(resolvedLayers);
                break;
            default:
                return TryAppendAdditionalClothingVisuals(wearer, clothingUid, slot, layers);
        }

        TryAppendAdditionalClothingVisuals(wearer, clothingUid, slot, layers);
        return layers.Count > 0;
    }

    private bool TryAppendAdditionalClothingVisuals(
        EntityUid wearer,
        EntityUid clothingUid,
        string slot,
        List<PrototypeLayerData> layers)
    {
        var visuals = new GetEquipmentVisualsEvent(wearer, slot);
        RaiseLocalEvent(clothingUid, visuals);

        foreach (var (key, layer) in visuals.Layers)
        {
            var copy = CopyLayer(layer);
            copy.MapKeys ??= new() { key };
            layers.Add(copy);
        }

        AppendUniformAccessoryVisuals(clothingUid, layers);
        AppendWebbingVisuals(clothingUid, layers);
        return visuals.Layers.Count > 0 || layers.Count > 0;
    }

    private void AppendUniformAccessoryVisuals(EntityUid clothingUid, List<PrototypeLayerData> layers)
    {
        if (!TryComp<UniformAccessoryHolderComponent>(clothingUid, out var holder) ||
            !_container.TryGetContainer(clothingUid, holder.ContainerId, out var container))
        {
            return;
        }

        var index = 0;
        foreach (var accessory in container.ContainedEntities)
        {
            if (!TryComp<UniformAccessoryComponent>(accessory, out var accessoryComp))
                continue;

            if (holder.HideAccessories && accessoryComp.HiddenByJacketRolling)
            {
                index++;
                continue;
            }

            if (accessoryComp.PlayerSprite is not { } sprite)
            {
                index++;
                continue;
            }

            layers.Add(new PrototypeLayerData
            {
                RsiPath = sprite.RsiPath.ToString(),
                State = sprite.RsiState,
                Visible = !accessoryComp.Hidden,
                MapKeys = new() { GetUniformAccessoryKey(accessory, accessoryComp, index) },
            });

            index++;
        }
    }

    private void AppendWebbingVisuals(EntityUid clothingUid, List<PrototypeLayerData> layers)
    {
        if (!TryComp<WebbingClothingComponent>(clothingUid, out var clothing) ||
            clothing.Webbing is not { } webbingUid ||
            !TryComp<WebbingComponent>(webbingUid, out var webbing) ||
            webbing.PlayerSprite is not { } sprite)
        {
            return;
        }

        var layer = clothing.Whitelist?.Tags?.Contains("ArmorWebbing") == true
            ? WebbingVisualLayers.Outer
            : WebbingVisualLayers.Base;

        layers.Add(new PrototypeLayerData
        {
            RsiPath = sprite.RsiPath.ToString(),
            State = sprite.RsiState,
            Visible = true,
            MapKeys = new() { $"enum.{nameof(WebbingVisualLayers)}.{layer}" },
        });
    }

    private string GetUniformAccessoryKey(EntityUid uid, UniformAccessoryComponent component, int index)
    {
        var key = $"enum.{nameof(UniformAccessoryLayer)}.{UniformAccessoryLayer.Base}{index}_{Name(uid)}_{uid.Id}";

        if (component.LayerKeys != null && component.LayerKeys.Count > 0 && component.Limit > 1)
        {
            var layerIndex = index < component.LayerKeys.Count ? index : component.LayerKeys.Count - 1;
            key = component.LayerKeys[layerIndex];
        }
        else if (component.LayerKeys != null && component.LayerKeys.Count == 1)
        {
            key = component.LayerKeys[0];
        }

        return key;
    }

    private bool TryGetInhandVisuals(
        ItemComponent item,
        HandLocation location,
        string? fallbackRsiPath,
        out List<PrototypeLayerData> layers)
    {
        var inhandVisuals = item.InhandVisuals;
        if (inhandVisuals.TryGetValue(location, out layers!))
            return true;

        return TryGetDefaultInhandVisuals(item, location, fallbackRsiPath, out layers);
    }

    private bool TryGetDefaultInhandVisuals(
        ItemComponent item,
        HandLocation location,
        string? fallbackRsiPath,
        out List<PrototypeLayerData> layers)
    {
        layers = new();

        if (fallbackRsiPath == null)
            return false;

        var defaultKey = $"inhand-{location.ToString().ToLowerInvariant()}";
        var state = item.HeldPrefix == null ? defaultKey : $"{item.HeldPrefix}-{defaultKey}";
        layers.Add(new PrototypeLayerData
        {
            RsiPath = fallbackRsiPath,
            State = state,
            MapKeys = new() { state },
        });

        return true;
    }

    private string? GetItemRsiPath(EntityUid itemUid, ItemComponent item)
    {
        if (!string.IsNullOrWhiteSpace(item.RsiPath))
            return item.RsiPath;

        if (!TryComp(itemUid, out MetaDataComponent? metaData) ||
            metaData.EntityPrototype?.ID is not { } prototypeId ||
            !TryGetPrototypeSpritePath(prototypeId, out var rsi))
        {
            return null;
        }

        return rsi;
    }

    private static DisplacementData? GetClothingDisplacement(
        InventoryComponent inventory,
        HumanoidAppearanceComponent humanoid,
        string slot)
    {
        return humanoid.Sex switch
        {
            Sex.Male when inventory.MaleDisplacements.Count > 0 => inventory.MaleDisplacements.GetValueOrDefault(slot),
            Sex.Female when inventory.FemaleDisplacements.Count > 0 => inventory.FemaleDisplacements.GetValueOrDefault(slot),
            _ => inventory.Displacements.GetValueOrDefault(slot),
        };
    }

    private static DisplacementData? GetHandDisplacement(HandsComponent hands, HandLocation location)
    {
        return location switch
        {
            HandLocation.Left when hands.LeftHandDisplacement != null => hands.LeftHandDisplacement,
            HandLocation.Middle or HandLocation.Right when hands.RightHandDisplacement != null => hands.RightHandDisplacement,
            _ => hands.HandDisplacement,
        };
    }

    private static void AddSnapshot(
        string key,
        PrototypeLayerData layer,
        DisplacementData? displacement,
        string? bookmarkKey,
        bool boostedAlpha,
        GhostHumanoidAppearanceComponent ghostAppearance)
    {
        ghostAppearance.Layers.Add(new GhostHumanoidLayerSnapshot
        {
            Key = key,
            BookmarkKey = bookmarkKey,
            Layer = CopyLayer(layer),
            Displacement = displacement == null ? null : CopyDisplacement(displacement),
            BoostedAlpha = boostedAlpha,
        });
    }

    private static RMCHumanoidAppearance SnapshotHumanoidAppearance(HumanoidAppearanceComponent sourceHumanoid)
    {
        return new RMCHumanoidAppearance
        {
            ClientOldMarkings = new(sourceHumanoid.ClientOldMarkings),
            MarkingSet = new(sourceHumanoid.MarkingSet),
            BaseLayers = new(sourceHumanoid.BaseLayers),
            PermanentlyHidden = new(sourceHumanoid.PermanentlyHidden),
            Gender = sourceHumanoid.Gender,
            Age = sourceHumanoid.Age,
            CustomBaseLayers = new(sourceHumanoid.CustomBaseLayers),
            Species = sourceHumanoid.Species,
            SkinColor = sourceHumanoid.SkinColor,
            HiddenLayers = new(sourceHumanoid.HiddenLayers),
            Sex = sourceHumanoid.Sex,
            EyeColor = sourceHumanoid.EyeColor,
            CachedHairColor = sourceHumanoid.CachedHairColor,
            CachedFacialHairColor = sourceHumanoid.CachedFacialHairColor,
            HideLayersOnEquip = new(sourceHumanoid.HideLayersOnEquip),
            UndergarmentTop = sourceHumanoid.UndergarmentTop,
            UndergarmentBottom = sourceHumanoid.UndergarmentBottom,
            MarkingsDisplacement = new(sourceHumanoid.MarkingsDisplacement),
        };
    }

    private static PrototypeLayerData CopyLayer(PrototypeLayerData layer)
    {
        return new PrototypeLayerData
        {
            Shader = layer.Shader,
            TexturePath = layer.TexturePath,
            RsiPath = layer.RsiPath,
            State = layer.State,
            Scale = layer.Scale,
            Rotation = layer.Rotation,
            Offset = layer.Offset,
            Visible = layer.Visible,
            Color = layer.Color,
            MapKeys = layer.MapKeys == null ? null : new(layer.MapKeys),
            RenderingStrategy = layer.RenderingStrategy,
            CopyToShaderParameters = layer.CopyToShaderParameters == null
                ? null
                : new()
                {
                    LayerKey = layer.CopyToShaderParameters.LayerKey,
                    ParameterTexture = layer.CopyToShaderParameters.ParameterTexture,
                    ParameterUV = layer.CopyToShaderParameters.ParameterUV,
                },
            Cycle = layer.Cycle,
        };
    }

    private static void PopulateFallbackRsiPath(PrototypeLayerData layer, string? fallbackRsiPath)
    {
        if (!string.IsNullOrWhiteSpace(layer.RsiPath) ||
            !string.IsNullOrWhiteSpace(layer.TexturePath) ||
            string.IsNullOrWhiteSpace(fallbackRsiPath))
        {
            return;
        }

        layer.RsiPath = fallbackRsiPath;
    }

    private static DisplacementData CopyDisplacement(DisplacementData displacement)
    {
        var copy = new DisplacementData
        {
            ShaderOverride = displacement.ShaderOverride,
        };

        foreach (var (size, layer) in displacement.SizeMaps)
        {
            copy.SizeMaps[size] = CopyLayer(layer);
        }

        return copy;
    }

    private string? GetClothingRsiPath(EntityUid clothingUid, ClothingComponent clothing)
    {
        if (!string.IsNullOrWhiteSpace(clothing.RsiPath))
            return clothing.RsiPath;

        if (!TryComp(clothingUid, out MetaDataComponent? metaData) ||
            metaData.EntityPrototype?.ID is not { } prototypeId ||
            !TryGetPrototypeSpritePath(prototypeId, out var rsi))
            return null;

        return rsi;
    }

    private bool TryGetPrototypeSpritePath(string prototypeId, out string? rsi)
    {
        return TryGetPrototypeSpritePath(prototypeId, new HashSet<string>(), out rsi);
    }

    private bool TryGetPrototypeSpritePath(string prototypeId, HashSet<string> visited, out string? rsi)
    {
        rsi = null;

        if (!visited.Add(prototypeId))
            return false;

        // Use the prototype mapping instead of EntityPrototype.Components so ignored client-only
        // components like Sprite still participate in ghost clothing fallback on the server.
        if (_prototypeManager.TryGetMapping(typeof(EntityPrototype), prototypeId, out var prototypeMapping) &&
            TryGetPrototypeComponentMapping(prototypeMapping, "Sprite", out var spriteMapping) &&
            spriteMapping.TryGet("sprite", out ValueDataNode? spriteNode) &&
            !string.IsNullOrWhiteSpace(spriteNode.Value))
        {
            rsi = spriteNode.Value;
            return true;
        }

        if (_prototypeManager.TryIndex<EntityPrototype>(prototypeId, out var prototype) &&
            prototype.Parents != null)
        {
            foreach (var parentId in prototype.Parents)
            {
                if (TryGetPrototypeSpritePath(parentId, visited, out rsi))
                    return true;
            }
        }

        return false;
    }

    private static bool TryGetPrototypeComponentMapping(
        MappingDataNode prototypeMapping,
        string componentName,
        out MappingDataNode componentMapping)
    {
        componentMapping = null!;

        if (!prototypeMapping.TryGet("components", out SequenceDataNode? components))
            return false;

        foreach (var node in components)
        {
            if (node is not MappingDataNode component ||
                !component.TryGet("type", out ValueDataNode? typeNode) ||
                typeNode.Value != componentName)
            {
                continue;
            }

            componentMapping = component;
            return true;
        }

        return false;
    }

    private bool RsiHasState(string? rsiPath, string state)
    {
        if (string.IsNullOrWhiteSpace(rsiPath))
            return false;

        var path = new ResPath(rsiPath);
        if (!_rsiStateCache.TryGetValue(path, out var states))
        {
            states = LoadRsiStates(path);
            _rsiStateCache[path] = states;
        }

        return states?.Contains(state) == true;
    }

    private HashSet<string>? LoadRsiStates(ResPath rsiPath)
    {
        var texturePath = rsiPath.IsRooted
            ? (rsiPath.CanonPath.StartsWith(SpriteSpecifierSerializer.TextureRoot.CanonPath)
                ? rsiPath
                : SpriteSpecifierSerializer.TextureRoot / rsiPath.ToRelativePath())
            : SpriteSpecifierSerializer.TextureRoot / rsiPath;

        if (!_resources.TryContentFileRead(texturePath / "meta.json", out var stream))
            return null;

        using (stream)
        {
            using var document = JsonDocument.Parse(stream);

            if (!document.RootElement.TryGetProperty("states", out var statesElement) ||
                statesElement.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            var states = new HashSet<string>();
            foreach (var state in statesElement.EnumerateArray())
            {
                if (state.TryGetProperty("name", out var name) &&
                    name.ValueKind == JsonValueKind.String &&
                    name.GetString() is { Length: > 0 } stateName)
                {
                    states.Add(stateName);
                }
            }

            return states;
        }
    }

    private bool TryCopyNonHumanoidDeathAppearance(EntityUid source, EntityUid ghost)
    {
        var ghostAppearance = EnsureComp<GhostNonHumanoidAppearanceComponent>(ghost);

        if (TryComp<GhostNonHumanoidAppearanceSourceComponent>(source, out var sourceAppearance))
        {
            ghostAppearance.Sprite = sourceAppearance.Sprite;
            ghostAppearance.State = sourceAppearance.State;
            ghostAppearance.SourcePrototype = null;
        }
        else if (TryComp(source, out MetaDataComponent? metaData) &&
                 metaData.EntityPrototype is { } prototype)
        {
            ghostAppearance.Sprite = null;
            ghostAppearance.State = null;
            ghostAppearance.SourcePrototype = prototype.ID;
        }
        else
        {
            return false;
        }

        ghostAppearance.SpentParasite = HasComp<ParasiteSpentComponent>(source);
        Dirty(ghost, ghostAppearance);

        return true;
    }
}
