using Content.Client.Clothing;
using Content.Client.Items.Systems;
using Content.Shared._RMC14.Webbing;
using Content.Shared.Clothing;
using Content.Shared.Inventory.Events;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using static Robust.Shared.Utility.SpriteSpecifier;

namespace Content.Client._RMC14.Webbing;

public sealed class WebbingSystem : SharedWebbingSystem
{
    [Dependency] private readonly ItemSystem _item = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public event Action? PlayerWebbingUpdated;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WebbingClothingComponent, AfterAutoHandleStateEvent>(OnClothingState);
        SubscribeLocalEvent<WebbingClothingComponent, GetEquipmentVisualsEvent>(OnWebbingClothingEquipmentVisuals,
            after: [typeof(ClientClothingSystem)]);
        SubscribeLocalEvent<WebbingClothingComponent, GotEquippedEvent>(OnClothingEquipped);
        SubscribeLocalEvent<WebbingClothingComponent, GotUnequippedEvent>(OnClothingUnequipped);

        SubscribeLocalEvent<WebbingTransferComponent, ComponentRemove>(OnWebbingTransferRemove);
    }

    private void OnWebbingClothingEquipmentVisuals(Entity<WebbingClothingComponent> ent, ref GetEquipmentVisualsEvent args)
    {
        if (!TryComp(ent.Comp.Webbing, out WebbingComponent? webbing))
        {
            return;
        }


        if (webbing.PlayerSprite == null && TryComp(ent.Comp.Webbing, out SpriteComponent? webbingSprite))
        {
            webbing.PlayerSprite = new(webbingSprite.BaseRSI?.Path ?? new ResPath("_RMC14/Objects/Clothing/Webbing/webbing.rsi"), "equipped");
        }

        if (webbing.PlayerSprite is not { } sprite)
        {
            return;
        }

        if (TryComp(ent, out SpriteComponent? clothingSprite) &&
                clothingSprite.LayerMapTryGet(WebbingVisualLayers.Base, out var clothingLayer))
            {
                clothingSprite.LayerSetVisible(clothingLayer, true);
                clothingSprite.LayerSetRSI(clothingLayer, sprite.RsiPath);
                clothingSprite.LayerSetState(clothingLayer, sprite.RsiState);
            }

        args.Layers.Add(($"enum.{nameof(WebbingVisualLayers)}.{nameof(WebbingVisualLayers.Base)}", new PrototypeLayerData
        {
            RsiPath = sprite.RsiPath.CanonPath,
            State = sprite.RsiState,
        }));
    }

    private void OnClothingState(Entity<WebbingClothingComponent> clothing, ref AfterAutoHandleStateEvent args)
    {
        if (TryComp(clothing, out SpriteComponent? clothingSprite) &&
            clothingSprite.LayerMapTryGet(WebbingVisualLayers.Base, out var clothingLayer))
        {
            if (TryComp(clothing.Comp.Webbing, out WebbingComponent? webbing) &&
                webbing.PlayerSprite is { } rsi)
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
        PlayerWebbingUpdated?.Invoke();
    }

    private void OnClothingEquipped(Entity<WebbingClothingComponent> clothing, ref GotEquippedEvent args)
    {
        if (_player.LocalEntity == args.Equipee)
            PlayerWebbingUpdated?.Invoke();
    }

    private void OnClothingUnequipped(Entity<WebbingClothingComponent> clothing, ref GotUnequippedEvent args)
    {
        if (_player.LocalEntity == args.Equipee)
            PlayerWebbingUpdated?.Invoke();
    }

    protected override void OnClothingInserted(Entity<WebbingClothingComponent> clothing, ref EntInsertedIntoContainerMessage args)
    {
        base.OnClothingInserted(clothing, ref args);

        if (_player.LocalEntity == args.Container.Owner)
            PlayerWebbingUpdated?.Invoke();
    }

    protected override void OnClothingRemoved(Entity<WebbingClothingComponent> clothing, ref EntRemovedFromContainerMessage args)
    {
        base.OnClothingRemoved(clothing, ref args);

        if (_player.LocalEntity == args.Container.Owner)
            PlayerWebbingUpdated?.Invoke();
    }

    private void OnWebbingTransferRemove(Entity<WebbingTransferComponent> ent, ref ComponentRemove args)
    {
        PlayerWebbingUpdated?.Invoke();
    }
}
