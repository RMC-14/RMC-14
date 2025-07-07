using System.Linq;
using Content.Client._RMC14.Attachable.Components;
using Content.Client._RMC14.Attachable.Systems;
using Content.Client.Clothing;
using Content.Client.Items.Systems;
using Content.Shared._RMC14.Attachable.Components;
using Content.Shared._RMC14.Item;
using Content.Shared.Clothing.Components;
using Content.Shared.Item;
using Content.Shared.Hands;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Utility;
using Content.Shared.Clothing;

namespace Content.Client._RMC14.Item;

public sealed class ItemCamouflageVisualizerSystem : VisualizerSystem<ItemCamouflageComponent>
{
    [Dependency] private readonly AttachableHolderVisualsSystem _attachableHolderVisuals = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly ItemSystem _item = default!;
    [Dependency] private readonly IResourceCache _resource = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ItemCamouflageComponent, GetInhandVisualsEvent>(OnGetInhandVisuals, after: [typeof(ItemSystem)]);
        SubscribeLocalEvent<ItemCamouflageComponent, GetEquipmentVisualsEvent>(OnGetClothingVisuals, after:[typeof(ClientClothingSystem)]);
    }

    // Add colour layer to in-hands of items that have a Camo Colour specified.
    private void OnGetInhandVisuals(EntityUid uid, ItemCamouflageComponent camoComp, GetInhandVisualsEvent args)
    {
        if (TryComp(uid, out AppearanceComponent? appearanceComponent))
        {
            AppearanceSystem.TryGetData(uid, ItemCamouflageVisuals.Camo, out CamouflageType camo, appearanceComponent);
            {
                if (camoComp.Colors != null)
                {
                    camoComp.Colors.TryGetValue(camo, out var camoColor);
                    {
                        var newLayer = new PrototypeLayerData();
                        foreach (var (state, layer) in args.Layers)
                        {
                            newLayer.RsiPath = layer.RsiPath;
                            newLayer.State = $"{state}-color";
                            newLayer.MapKeys = new() { $"{state}-color" };
                            newLayer.Color = camoColor;
                        }
                        if (newLayer.State is not null)
                        {
                            args.Layers.Add((newLayer.State, newLayer));
                        }
                    }
                }
            }
        }
    }

    // Add colour layer to clothing of items that have a Camo Colour specified.
    private void OnGetClothingVisuals(EntityUid uid, ItemCamouflageComponent camoComp, GetEquipmentVisualsEvent args)
    {
        if (TryComp(uid, out AppearanceComponent? appearanceComponent))
        {
            AppearanceSystem.TryGetData(uid, ItemCamouflageVisuals.Camo, out CamouflageType camo, appearanceComponent);
            {
                if (camoComp.Colors != null)
                {
                    camoComp.Colors.TryGetValue(camo, out var camoColor);
                    {
                        var newLayer = new PrototypeLayerData();
                        foreach (var (state, layer) in args.Layers)
                        {
                            newLayer.RsiPath = layer.RsiPath;
                            newLayer.State = $"equipped-{args.Slot.ToUpper()}-color";
                            newLayer.MapKeys = new() { $"equipped-{args.Slot.ToUpper()}-color" };
                            newLayer.Color = camoColor;
                        }
                        if (newLayer.State is not null)
                        {
                            args.Layers.Add((newLayer.State, newLayer));
                        }
                    }
                }
            }
        }
    }

    protected override void OnAppearanceChange(EntityUid uid, ItemCamouflageComponent component, ref AppearanceChangeEvent args)
    {
        base.OnAppearanceChange(uid, component, ref args);

        if (!AppearanceSystem.TryGetData(uid, ItemCamouflageVisuals.Camo, out CamouflageType camo, args.Component))
            return;

        if (component.CamouflageVariations != null && component.CamouflageVariations.TryGetValue(camo, out var rsi))
        {
            if (args.Sprite != null)
            {
                if (args.Sprite.LayerMapTryGet(ItemCamouflageLayers.Layer, out var layer))
                {
                    args.Sprite.LayerSetRSI(layer, rsi);
                }
                else if (args.Sprite.BaseRSI != null &&
                         _resource.TryGetResource(SpriteSpecifierSerializer.TextureRoot / rsi, out RSIResource? baseRsi))
                {
                    args.Sprite.BaseRSI = baseRsi.RSI;
                }
            }

            if (TryComp(uid, out ClothingComponent? clothing))
#pragma warning disable RA0002
                clothing.RsiPath = rsi.ToString();
#pragma warning restore RA0002

            if (TryComp(uid, out ItemComponent? item))
#pragma warning disable RA0002
                item.RsiPath = rsi.ToString();
#pragma warning restore RA0002

            if (TryComp(uid, out AttachableToggleableComponent? toggleable))
            {
                if (toggleable.Icon is SpriteSpecifier.Rsi toggleableRsi)
#pragma warning disable RA0002
                    toggleable.Icon = new SpriteSpecifier.Rsi(rsi, toggleableRsi.RsiState);
#pragma warning restore RA0002

                if (toggleable.IconActive is SpriteSpecifier.Rsi toggleableActiveRsi)
#pragma warning disable RA0002
                    toggleable.IconActive = new SpriteSpecifier.Rsi(rsi, toggleableActiveRsi.RsiState);
#pragma warning restore RA0002
            }

            if (TryComp(uid, out AttachableVisualsComponent? visuals))
            {
                if (visuals.Rsi != null)
#pragma warning disable RA0002
                    visuals.Rsi = rsi;
#pragma warning restore RA0002

                if (visuals.LastSlotId != null &&
                    visuals.LastSuffix != null &&
                    _container.TryGetContainingContainer((uid, null), out var container) &&
                    TryComp(container.Owner, out AttachableHolderVisualsComponent? holder))
                {
                    _attachableHolderVisuals.RefreshVisuals((container.Owner, holder), (uid, visuals), visuals.LastSlotId, visuals.LastSuffix);
                }
            }
        }

        if (component.States != null && component.States.TryGetValue(camo, out var state))
        {
            args.Sprite?.LayerSetState(0, state);

            if (TryComp(uid, out AttachableToggleableComponent? toggleable))
            {
                if (toggleable.Icon is SpriteSpecifier.Rsi toggleableRsi)
#pragma warning disable RA0002
                    toggleable.Icon = new SpriteSpecifier.Rsi(toggleableRsi.RsiPath, state);
#pragma warning restore RA0002

                if (toggleable.IconActive is SpriteSpecifier.Rsi toggleableActiveRsi)
#pragma warning disable RA0002
                    toggleable.IconActive = new SpriteSpecifier.Rsi(toggleableActiveRsi.RsiPath, state);
#pragma warning restore RA0002
            }
        }

        if (component.Colors != null && component.Colors.TryGetValue(camo, out var color))
        {
            if (args.Sprite != null)
            {
                foreach (var camoLayer in Enum.GetValues(typeof(ItemCamouflageLayers)))
                {
                    if (args.Sprite.LayerMapTryGet(camoLayer, out var layer))
                    {
                        args.Sprite.LayerSetColor(layer, color);
                    }
                }
            }
        }

        if (component.Layers != null && args.Sprite != null)
        {
            foreach (var (key, layerCamos) in component.Layers)
            {
                if (layerCamos.TryGetValue(camo, out var layerState) &&
                    args.Sprite.LayerMapTryGet(key, out var layer))
                {
                    args.Sprite.LayerSetState(layer, layerState);
                }
            }
        }

        _item.VisualsChanged(uid);
    }
}
