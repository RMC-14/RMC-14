using System.Linq;
using Content.Shared._RMC14.Humanoid;
using Content.Shared._RMC14.UniformAccessories;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Clothing;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Item;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using static Robust.Shared.Utility.SpriteSpecifier;

namespace Content.Client._RMC14.UniformAccessories;

public sealed class UniformAccessorySystem : SharedUniformAccessorySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly RMCHumanoidAppearanceSystem _rmcHumanoid = default!;

    public event Action? PlayerMedalUpdated;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UniformAccessoryHolderComponent, GetEquipmentVisualsEvent>(OnHolderGetEquipmentVisuals, after: [typeof(ClothingSystem)]);
        SubscribeLocalEvent<UniformAccessoryHolderComponent, AfterAutoHandleStateEvent>(OnHolderAfterState);
        SubscribeLocalEvent<UniformAccessoryHolderComponent, EntInsertedIntoContainerMessage>(OnHolderInsertedContainer);
        SubscribeLocalEvent<UniformAccessoryHolderComponent, EntRemovedFromContainerMessage>(OnHolderRemovedContainer);
        SubscribeLocalEvent<UniformAccessoryHolderComponent, EquipmentVisualsUpdatedEvent>(OnHolderVisualsUpdated, after: [typeof(ClothingSystem)]);
    }

    private void OnHolderGetEquipmentVisuals(Entity<UniformAccessoryHolderComponent> ent, ref GetEquipmentVisualsEvent args)
    {
        if (_rmcHumanoid.HidePlayerIdentities && HasComp<XenoComponent>(_player.LocalEntity))
            return;

        var clothingSprite = CompOrNull<SpriteComponent>(ent);

        if (!_container.TryGetContainer(ent, ent.Comp.ContainerId, out var container))
            return;

        var index = 0;
        foreach (var accessory in container.ContainedEntities)
        {
            if (!TryComp<UniformAccessoryComponent>(accessory, out var accessoryComp))
                continue;

            var layer = GetKey(accessory, accessoryComp, index);

            if (accessoryComp.PlayerSprite == null && TryComp(accessory, out SpriteComponent? accessorySprite))
            {
                accessoryComp.PlayerSprite = new(accessorySprite.BaseRSI?.Path ?? new ResPath("_RMC14/Objects/Medals/bronze.rsi"), "equipped");
            }

            if (accessoryComp.PlayerSprite is not { } sprite)
                continue;

            if (ent.Comp.HideAccessories && accessoryComp.HiddenByJacketRolling)
                continue;

            if (clothingSprite != null && accessoryComp.HasIconSprite)
            {
                var clothingLayer = clothingSprite.LayerMapReserveBlank(layer);
                clothingSprite.LayerSetVisible(clothingLayer, !accessoryComp.Hidden);
                clothingSprite.LayerSetRSI(clothingLayer, sprite.RsiPath);
                clothingSprite.LayerSetState(clothingLayer, sprite.RsiState);
            }

            if (args.Layers.Any(t => t.Item1 == layer))
                continue;

            args.Layers.Add((layer, new PrototypeLayerData
            {
                RsiPath = sprite.RsiPath.ToString(),
                State = sprite.RsiState,
                Visible = !accessoryComp.Hidden
            }));

            index++;
        }

        PlayerMedalUpdated?.Invoke();
    }

    private void OnHolderAfterState(Entity<UniformAccessoryHolderComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        _item.VisualsChanged(ent);
    }

    private void OnHolderInsertedContainer(Entity<UniformAccessoryHolderComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        _item.VisualsChanged(ent);
    }

    private void OnHolderRemovedContainer(Entity<UniformAccessoryHolderComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        var item = args.Entity;

        if (!TryComp<UniformAccessoryComponent>(item, out var accessoryComp))
            return;

        var index = 0;
        foreach (var accessory in args.Container.ContainedEntities)
        {
            if (accessory == item)
                break;

            index++;
        }

        var layer = GetKey(item, accessoryComp, index);

        if (TryComp(ent.Owner, out SpriteComponent? clothingSprite) && clothingSprite.LayerMapTryGet(layer, out var clothingLayer))
            clothingSprite.LayerSetVisible(clothingLayer, false);

        _item.VisualsChanged(ent);
    }

    private void OnHolderVisualsUpdated(Entity<UniformAccessoryHolderComponent> ent, ref EquipmentVisualsUpdatedEvent args)
    {
        if (_rmcHumanoid.HidePlayerIdentities && HasComp<XenoComponent>(_player.LocalEntity))
            return;

        if (!_container.TryGetContainer(ent, ent.Comp.ContainerId, out var container))
            return;

        var key = string.Empty;
        foreach (var accessory in container.ContainedEntities)
        {
            if (!TryComp<UniformAccessoryComponent>(accessory, out var accessoryComp))
                return;

            if (accessoryComp.PlayerSprite == null && TryComp(accessory, out SpriteComponent? accessorySprite))
            {
                accessoryComp.PlayerSprite = new(accessorySprite.BaseRSI?.Path ?? new ResPath("_RMC14/Objects/Medals/bronze.rsi"), "equipped");
            }

            if (accessoryComp.LayerKey != null)
                key = accessoryComp.LayerKey;
        }

        if (key == string.Empty)
            return;

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

    private string GetKey(EntityUid uid, UniformAccessoryComponent component, int index)
    {
        var key = $"enum.{nameof(UniformAccessoryLayer)}.{UniformAccessoryLayer.Base}{index}_{Name(uid)}_{uid.Id}";

        if (component.LayerKey != null)
            key = component.LayerKey;

        return key;
    }
}
