using Content.Shared.Actions;
using Content.Shared.Clothing.Components;
using Content.Shared.Item.ItemToggle.Components;

namespace Content.Shared._RMC14.Clothing;

//    Keeps the toggled state of "ToggleClothingComponent"action in sync with the
//    item's "ItemToggleComponent" state, so actions with an icon/iconOn pair swap correctly.
public sealed class RMCToggleClothingActionSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ToggleClothingComponent, ItemToggledEvent>(OnItemToggled);
    }

    private void OnItemToggled(Entity<ToggleClothingComponent> ent, ref ItemToggledEvent args)
    {
        _actions.SetToggled(ent.Comp.ActionEntity, args.Activated);
    }
}
