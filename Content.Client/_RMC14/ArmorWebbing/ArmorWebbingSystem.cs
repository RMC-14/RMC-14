using Content.Client.Clothing;
using Content.Client.Items.Systems;
using Content.Shared._RMC14.ArmorWebbing;
using Content.Shared.Clothing;
using Content.Shared.Inventory.Events;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using static Robust.Shared.Utility.SpriteSpecifier;

namespace Content.Client._RMC14.ArmorWebbing;

public sealed class ArmorWebbingSystem : SharedArmorWebbingSystem
{
    [Dependency] private readonly ItemSystem _item = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public event Action? PlayerArmorWebbingUpdated;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ArmorWebbingClothingComponent, AfterAutoHandleStateEvent>(OnClothingState);
        SubscribeLocalEvent<ArmorWebbingClothingComponent, GetEquipmentVisualsEvent>(OnArmorWebbingClothingEquipmentVisuals,
            after: [typeof(ClientClothingSystem)]);
        SubscribeLocalEvent<ArmorWebbingClothingComponent, GotEquippedEvent>(OnClothingEquipped);
        SubscribeLocalEvent<ArmorWebbingClothingComponent, GotUnequippedEvent>(OnClothingUnequipped);

        SubscribeLocalEvent<ArmorWebbingTransferComponent, ComponentRemove>(OnArmorWebbingTransferRemove);
    }

    private void OnArmorWebbingClothingEquipmentVisuals(Entity<ArmorWebbingClothingComponent> ent, ref GetEquipmentVisualsEvent args)
    {
        if (!TryComp(ent.Comp.ArmorWebbing, out ArmorWebbingComponent? armorWebbing))
        {
            return;
        }


        if (armorWebbing.PlayerSprite == null && TryComp(ent.Comp.ArmorWebbing, out SpriteComponent? armorWebbingSprite))
        {
            armorWebbing.PlayerSprite = new(armorWebbingSprite.BaseRSI?.Path ?? new ResPath("_RMC14/Objects/Clothing/ArmorWebbing/pouch.rsi"), "equipped");
        }

        if (armorWebbing.PlayerSprite is not { } sprite)
        {
            return;
        }

        if (TryComp(ent, out SpriteComponent? clothingSprite) &&
                clothingSprite.LayerMapTryGet(ArmorWebbingVisualLayers.Base, out var clothingLayer))
            {
                clothingSprite.LayerSetVisible(clothingLayer, true);
                clothingSprite.LayerSetRSI(clothingLayer, sprite.RsiPath);
                clothingSprite.LayerSetState(clothingLayer, sprite.RsiState);
            }

        args.Layers.Add(($"enum.{nameof(ArmorWebbingVisualLayers)}.{nameof(ArmorWebbingVisualLayers.Base)}", new PrototypeLayerData
        {
            RsiPath = sprite.RsiPath.CanonPath,
            State = sprite.RsiState,
        }));
    }

    private void OnClothingState(Entity<ArmorWebbingClothingComponent> clothing, ref AfterAutoHandleStateEvent args)
    {
        if (TryComp(clothing, out SpriteComponent? clothingSprite) &&
            clothingSprite.LayerMapTryGet(ArmorWebbingVisualLayers.Base, out var clothingLayer))
        {
            if (TryComp(clothing.Comp.ArmorWebbing, out ArmorWebbingComponent? armorWebbing) &&
                armorWebbing.PlayerSprite is { } rsi)
            {
                clothingSprite.LayerSetVisible(clothingLayer, true);
                clothingSprite.LayerSetRSI(clothingLayer, rsi.RsiPath);
                clothingSprite.LayerSetState(clothingLayer, rsi.RsiState);
            }
            else
            {
                clothingSprite.LayerSetVisible(clothingLayer, false);
            }
        }

        _item.VisualsChanged(clothing);
        PlayerArmorWebbingUpdated?.Invoke();
    }

    private void OnClothingEquipped(Entity<ArmorWebbingClothingComponent> clothing, ref GotEquippedEvent args)
    {
        if (_player.LocalEntity == args.Equipee)
            PlayerArmorWebbingUpdated?.Invoke();
    }

    private void OnClothingUnequipped(Entity<ArmorWebbingClothingComponent> clothing, ref GotUnequippedEvent args)
    {
        if (_player.LocalEntity == args.Equipee)
            PlayerArmorWebbingUpdated?.Invoke();
    }

    protected override void OnClothingInserted(Entity<ArmorWebbingClothingComponent> clothing, ref EntInsertedIntoContainerMessage args)
    {
        base.OnClothingInserted(clothing, ref args);

        if (_player.LocalEntity == args.Container.Owner)
            PlayerArmorWebbingUpdated?.Invoke();
    }

    protected override void OnClothingRemoved(Entity<ArmorWebbingClothingComponent> clothing, ref EntRemovedFromContainerMessage args)
    {
        base.OnClothingRemoved(clothing, ref args);

        if (_player.LocalEntity == args.Container.Owner)
            PlayerArmorWebbingUpdated?.Invoke();
    }

    private void OnArmorWebbingTransferRemove(Entity<ArmorWebbingTransferComponent> ent, ref ComponentRemove args)
    {
        PlayerArmorWebbingUpdated?.Invoke();
    }
}
