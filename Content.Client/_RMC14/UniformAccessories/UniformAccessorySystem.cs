using System.Linq;
using Content.Shared._RMC14.Humanoid;
using Content.Shared._RMC14.UniformAccessories;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Clothing;
using Content.Shared.Clothing.EntitySystems;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Containers;

namespace Content.Client._RMC14.UniformAccessories;

public sealed class UniformAccessorySystem : SharedUniformAccessorySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly RMCHumanoidAppearanceSystem _rmcHumanoid = default!;

    public event Action? PlayerMedalUpdated;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<UniformAccessoryHolderComponent, GetEquipmentVisualsEvent>(OnHolderGetEquipmentVisuals, after: [typeof(ClothingSystem)]);
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
}
