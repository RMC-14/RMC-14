using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Item;
using Content.Shared.Item.ItemToggle.Components;

namespace Content.Shared._RMC14.Item.ItemToggle;

public sealed class RMCItemToggleSystem : EntitySystem
{
    [Dependency] private readonly ClothingSystem _clothing = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;

    private EntityQuery<ItemToggleComponent> _query;

    public override void Initialize()
    {
        base.Initialize();

        _query = GetEntityQuery<ItemToggleComponent>();

        SubscribeLocalEvent<RMCItemToggleClothingVisualsComponent, ItemToggledEvent>(OnToggled);
    }

    private void OnToggled(Entity<RMCItemToggleClothingVisualsComponent> ent, ref ItemToggledEvent args)
    {
        var prefix = args.Activated ? ent.Comp.Prefix : null;
        _item.SetHeldPrefix(ent, prefix);
        _clothing.SetEquippedPrefix(ent, prefix);
    }
}
