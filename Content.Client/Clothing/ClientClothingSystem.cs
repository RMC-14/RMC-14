using System.Linq;
using Content.Client.DisplacementMap;
using Content.Client.Inventory;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Utility;
using static Robust.Client.GameObjects.SpriteComponent;

namespace Content.Client.Clothing;

public sealed class ClientClothingSystem : ClothingSystem
{
    public const string Jumpsuit = "jumpsuit";

    [Dependency] private readonly IResourceCache _cache = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly DisplacementMapSystem _displacement = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClothingComponent, GetEquipmentVisualsEvent>(OnGetVisuals);
        SubscribeLocalEvent<ClothingComponent, InventoryTemplateUpdated>(OnInventoryTemplateUpdated);

        SubscribeLocalEvent<InventoryComponent, VisualsChangedEvent>(OnVisualsChanged);
        SubscribeLocalEvent<SpriteComponent, DidUnequipEvent>(OnDidUnequip);
        SubscribeLocalEvent<InventoryComponent, AppearanceChangeEvent>(OnAppearanceUpdate);
    }

    private void OnAppearanceUpdate(EntityUid uid, InventoryComponent component, ref AppearanceChangeEvent args)
    {
        // May need to update displacement maps if the sex changed. Also required to properly set the stencil on init
        if (args.Sprite == null)
            return;

        UpdateAllSlots(uid, component);

        // No clothing equipped -> make sure the layer is hidden, though this should already be handled by on-unequip.
        if (_sprite.LayerMapTryGet((uid, args.Sprite), HumanoidVisualLayers.StencilMask, out var layer, false))
        {
            DebugTools.Assert(!args.Sprite[layer].Visible);
            _sprite.LayerSetVisible((uid, args.Sprite), layer, false);
        }
    }

    private void OnInventoryTemplateUpdated(Entity<ClothingComponent> ent, ref InventoryTemplateUpdated args)
    {
        UpdateAllSlots(ent.Owner, clothing: ent.Comp);
    }

    private void UpdateAllSlots(
        EntityUid uid,
        InventoryComponent? inventoryComponent = null,
        ClothingComponent? clothing = null)
    {
        var enumerator = _inventorySystem.GetSlotEnumerator((uid, inventoryComponent));
        while (enumerator.NextItem(out var item, out var slot))
        {
            RenderEquipment(uid, item, slot.Name, inventoryComponent, clothingComponent: clothing);
        }
    }

    private void OnGetVisuals(EntityUid uid, ClothingComponent item, GetEquipmentVisualsEvent args)
    {
        if (!TryComp(args.Equipee, out InventoryComponent? inventory))
            return;

        RSI? rsi = null;
        if (item.RsiPath != null)
            rsi = _cache.GetResource<RSIResource>(SpriteSpecifierSerializer.TextureRoot / item.RsiPath).RSI;
        else if (TryComp(uid, out SpriteComponent? sprite))
            rsi = sprite.BaseRSI;

        var rsiPath = rsi?.Path.ToString();
        var resolution = ResolveEquippedVisuals(
            item,
            args.Slot,
            inventory.SpeciesId,
            rsiPath,
            state => rsi != null && rsi.TryGetState(state, out _),
            out var layers);

        if (resolution == ClothingVisualResolution.None || layers == null)
        {
            return;
        }

        // add each layer to the visuals
        var i = 0;
        foreach (var layer in layers)
        {
            var key = layer.MapKeys?.FirstOrDefault();
            if (key == null)
            {
                // using the $"{args.Slot}" layer key as the "bookmark" for layer ordering until layer draw depths get added
                key = $"{args.Slot}-{i}";
                i++;
            }

            item.MappedLayer = key;
            args.Layers.Add((key, layer));
        }
    }

    private void OnVisualsChanged(EntityUid uid, InventoryComponent component, VisualsChangedEvent args)
    {
        var item = GetEntity(args.Item);

        if (!TryComp(item, out ClothingComponent? clothing) || clothing.InSlot == null)
            return;

        RenderEquipment(uid, item, clothing.InSlot, component, null, clothing);
    }

    private void OnDidUnequip(Entity<SpriteComponent> entity, ref DidUnequipEvent args)
    {
        if (!TryComp(entity, out InventorySlotsComponent? inventorySlots))
            return;

        if (!inventorySlots.VisualLayerKeys.TryGetValue(args.Slot, out var revealedLayers))
            return;

        // Remove old layers. We could also just set them to invisible, but as items may add arbitrary layers, this
        // may eventually bloat the player with lots of invisible layers.
        foreach (var layer in revealedLayers)
        {
            // RMC14
            try
            {
                _sprite.RemoveLayer(entity.AsNullable(), layer);
            }
            catch (Exception e)
            {
                Log.Error($"Error removing layer:\n{e}");
            }
        }
        revealedLayers.Clear();
    }

    public void InitClothing(EntityUid uid, InventoryComponent component)
    {
        if (!TryComp(uid, out SpriteComponent? sprite))
            return;

        var enumerator = _inventorySystem.GetSlotEnumerator((uid, component));
        while (enumerator.NextItem(out var item, out var slot))
        {
            RenderEquipment(uid, item, slot.Name, component, sprite);
        }
    }

    protected override void OnGotEquipped(EntityUid uid, ClothingComponent component, GotEquippedEvent args)
    {
        base.OnGotEquipped(uid, component, args);

        RenderEquipment(args.Equipee, uid, args.Slot, clothingComponent: component);
    }

    private void RenderEquipment(EntityUid equipee, EntityUid equipment, string slot,
        InventoryComponent? inventory = null, SpriteComponent? sprite = null, ClothingComponent? clothingComponent = null,
        InventorySlotsComponent? inventorySlots = null)
    {
        if (!Resolve(equipee, ref inventory, ref sprite, ref inventorySlots) ||
           !Resolve(equipment, ref clothingComponent, false))
        {
            return;
        }

        if (!_inventorySystem.TryGetSlot(equipee, slot, out var slotDef, inventory))
            return;

        // Remove old layers. We could also just set them to invisible, but as items may add arbitrary layers, this
        // may eventually bloat the player with lots of invisible layers.
        if (inventorySlots.VisualLayerKeys.TryGetValue(slot, out var revealedLayers))
        {
            foreach (var key in revealedLayers)
            {
                _sprite.RemoveLayer((equipee, sprite), key);
            }
            revealedLayers.Clear();
        }
        else
        {
            revealedLayers = new();
            inventorySlots.VisualLayerKeys[slot] = revealedLayers;
        }

        var ev = new GetEquipmentVisualsEvent(equipee, slot);
        RaiseLocalEvent(equipment, ev);

        if (ev.Layers.Count == 0)
        {
            RaiseLocalEvent(equipment, new EquipmentVisualsUpdatedEvent(equipee, slot, revealedLayers), true);
            return;
        }

        // temporary, until layer draw depths get added. Basically: a layer with the key "slot" is being used as a
        // bookmark to determine where in the list of layers we should insert the clothing layers.
        var slotLayerExists = _sprite.LayerMapTryGet((equipee, sprite), slot, out var index, false);

        // Select displacement maps
        var displacementData = inventory.Displacements.GetValueOrDefault(slot); //Default unsexed map

        var equipeeSex = CompOrNull<HumanoidAppearanceComponent>(equipee)?.Sex;
        if (equipeeSex != null)
        {
            switch (equipeeSex)
            {
                case Sex.Male:
                    if (inventory.MaleDisplacements.Count > 0)
                        displacementData = inventory.MaleDisplacements.GetValueOrDefault(slot);
                    break;
                case Sex.Female:
                    if (inventory.FemaleDisplacements.Count > 0)
                        displacementData = inventory.FemaleDisplacements.GetValueOrDefault(slot);
                    break;
            }
        }

        // add the new layers
        foreach (var (key, layerData) in ev.Layers)
        {
            if (!revealedLayers.Add(key))
            {
                Log.Warning($"Duplicate key for clothing visuals: {key}. Are multiple components attempting to modify the same layer? Equipment: {ToPrettyString(equipment)}");
                continue;
            }

            if (slotLayerExists)
            {
                index++;
                // note that every insertion requires reshuffling & remapping all the existing layers.
                _sprite.AddBlankLayer((equipee, sprite), index);
                _sprite.LayerMapRemove((equipee, sprite), key); // RMC14
                _sprite.LayerMapSet((equipee, sprite), key, index);

                if (layerData.Color != null)
                    _sprite.LayerSetColor((equipee, sprite), key, layerData.Color.Value);
                if (layerData.Scale != null)
                    _sprite.LayerSetScale((equipee, sprite), key, layerData.Scale.Value);
            }
            else
                index = _sprite.LayerMapReserve((equipee, sprite), key);

            if (sprite[index] is not Layer layer)
                continue;

            // In case no RSI is given, use the item's base RSI as a default. This cuts down on a lot of unnecessary yaml entries.
            if (layerData.RsiPath == null
                && layerData.TexturePath == null
                && layer.RSI == null
                && TryComp(equipment, out SpriteComponent? clothingSprite))
            {
                _sprite.LayerSetRsi(layer, clothingSprite.BaseRSI);
            }

            _sprite.LayerSetData((equipee, sprite), index, layerData);
            _sprite.LayerSetOffset(layer, layer.Offset + slotDef.Offset);

            if (displacementData is not null)
            {
                //Checking that the state is not tied to the current race. In this case we don't need to use the displacement maps.
                if (layerData.State is not null && inventory.SpeciesId is not null && layerData.State.EndsWith(inventory.SpeciesId))
                    continue;

                if (_displacement.TryAddDisplacement(displacementData, (equipee, sprite), index, key, out var displacementKey))
                {
                    revealedLayers.Add(displacementKey);
                    index++;
                }
            }
        }

        RaiseLocalEvent(equipment, new EquipmentVisualsUpdatedEvent(equipee, slot, revealedLayers), true);
    }
}
