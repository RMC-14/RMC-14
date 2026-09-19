using Content.Shared._RMC14.Inventory;
using Content.Shared.Clothing.Components;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Input.Binding;

namespace Content.Shared._RMC14.Input;

public sealed class RMCInventoryKeybindSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedCMInventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder
            .Bind(CMKeyFunctions.RMCQuickEquipInventory,
                InputCmdHandler.FromDelegate(session =>
                {
                    if (session?.AttachedEntity is { } user)
                        TryQuickEquipInventory(user);
                }, handle: false))
            .Register<RMCInventoryKeybindSystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        CommandBinds.Unregister<RMCInventoryKeybindSystem>();
    }

    public bool TryQuickEquipInventory(EntityUid user)
    {
        return _hands.TryGetActiveItem(user, out var held) &&
               TryComp(held, out ClothingComponent? clothing) &&
               _inventory.TryEquipClothing(user, (held.Value, clothing), false);
    }
}
