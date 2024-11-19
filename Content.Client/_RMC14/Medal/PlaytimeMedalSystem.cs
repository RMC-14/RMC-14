using System.Linq;
using Content.Shared._RMC14.Medal;
using Content.Shared.Clothing;
using Content.Shared.Clothing.EntitySystems;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Medal;

public sealed class PlaytimeMedalSystem : SharedPlaytimeMedalSystem
{
    public event Action? PlayerMedalUpdated;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlaytimeMedalHolderComponent, GetEquipmentVisualsEvent>(OnPlaytimeMedalHolderGetEquipmentVisuals, after: [typeof(ClothingSystem)]);
    }

    private void OnPlaytimeMedalHolderGetEquipmentVisuals(Entity<PlaytimeMedalHolderComponent> ent, ref GetEquipmentVisualsEvent args)
    {
        var clothingSprite = CompOrNull<SpriteComponent>(ent);
        if (!TryComp(ent.Comp.Medal, out PlaytimeMedalComponent? medal) ||
            medal.PlayerSprite is not { } sprite)
        {
            if (clothingSprite != null &&
                clothingSprite.LayerMapTryGet(MedalVisualLayers.Base, out var medalLayer))
            {
                clothingSprite.LayerSetVisible(medalLayer, false);
            }

            PlayerMedalUpdated?.Invoke();
            return;
        }

        if (clothingSprite != null)
        {
            var clothingLayer = clothingSprite.LayerMapReserveBlank(MedalVisualLayers.Base);
            clothingSprite.LayerSetVisible(clothingLayer, true);
            clothingSprite.LayerSetRSI(clothingLayer, sprite.RsiPath);
            clothingSprite.LayerSetState(clothingLayer, sprite.RsiState);
        }

        PlayerMedalUpdated?.Invoke();

        var key = $"enum.{nameof(MedalVisualLayers)}.{nameof(MedalVisualLayers.Base)}";
        if (args.Layers.Any(t => t.Item1 == key))
            return;

        args.Layers.Add((key, new PrototypeLayerData
        {
            RsiPath = sprite.RsiPath.CanonPath,
            State = sprite.RsiState,
        }));
    }
}
