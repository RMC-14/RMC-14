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
            var layer = $"enum.{nameof(UniformAccessoryLayer)}.{UniformAccessoryLayer.Base}{index}_{Name(ent.Owner)}";

            if (!TryComp<UniformAccessoryComponent>(accessory, out var accessoryComp))
                continue;

            if (accessoryComp.PlayerSprite is not { } sprite)
                continue;

            if (clothingSprite != null)
            {
                var clothingLayer = clothingSprite.LayerMapReserveBlank(layer);
                clothingSprite.LayerSetVisible(clothingLayer, true);
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

        var index = 0;
        foreach (var accessory in args.Container.ContainedEntities)
        {
            if (accessory != item)
                break;

            index++;
        }

        var layer = $"enum.{nameof(UniformAccessoryLayer)}.{UniformAccessoryLayer.Base}{index}_{Name(item)}";

        if (TryComp(ent.Owner, out SpriteComponent? clothingSprite) && clothingSprite.LayerMapTryGet(layer, out var clothingLayer))
            clothingSprite.LayerSetVisible(clothingLayer, false);

        _item.VisualsChanged(ent);
    }
}
