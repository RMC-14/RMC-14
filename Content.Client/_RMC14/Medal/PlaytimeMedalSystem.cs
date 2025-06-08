using System.Linq;
using Content.Shared._RMC14.Humanoid;
using Content.Shared._RMC14.Medal;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Clothing;
using Content.Shared.Clothing.EntitySystems;
using Robust.Client.GameObjects;
using Robust.Client.Player;

namespace Content.Client._RMC14.Medal;

public sealed class PlaytimeMedalSystem : SharedPlaytimeMedalSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly RMCHumanoidAppearanceSystem _rmcHumanoid = default!;

    public event Action? PlayerMedalUpdated;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlaytimeMedalHolderComponent, GetEquipmentVisualsEvent>(OnHolderGetEquipmentVisuals, after: [typeof(ClothingSystem)]);
        SubscribeLocalEvent<PlaytimeMedalHolderComponent, EquipmentVisualsUpdatedEvent>(OnHolderVisualsUpdated, after: [typeof(ClothingSystem)]);
    }

    private void OnHolderGetEquipmentVisuals(Entity<PlaytimeMedalHolderComponent> ent, ref GetEquipmentVisualsEvent args)
    {
        if (_rmcHumanoid.HidePlayerIdentities && HasComp<XenoComponent>(_player.LocalEntity))
            return;

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

        var key = GetKey();
        if (args.Layers.Any(t => t.Item1 == key))
            return;

        args.Layers.Add((key, new PrototypeLayerData
        {
            RsiPath = sprite.RsiPath.CanonPath,
            State = sprite.RsiState,
        }));
    }

    private void OnHolderVisualsUpdated(Entity<PlaytimeMedalHolderComponent> ent, ref EquipmentVisualsUpdatedEvent args)
    {
        if (_rmcHumanoid.HidePlayerIdentities && HasComp<XenoComponent>(_player.LocalEntity))
            return;

        var key = GetKey();
        if (!args.RevealedLayers.Contains(key))
            return;

        if (!TryComp(args.Equipee, out SpriteComponent? sprite))
            return;

        if (!sprite.LayerMapTryGet(key, out var layer) ||
            !sprite.TryGetLayer(layer, out var layerData))
        {
            return;
        }

        var data = layerData.ToPrototypeData();
        sprite.RemoveLayer(layer);

        layer = sprite.LayerMapReserveBlank(key);
        sprite.LayerSetData(layer, data);
    }

    private string GetKey()
    {
        return $"enum.{nameof(MedalVisualLayers)}.{nameof(MedalVisualLayers.Base)}";
    }
}
