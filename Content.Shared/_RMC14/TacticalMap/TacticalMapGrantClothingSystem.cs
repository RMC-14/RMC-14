using Content.Shared.Clothing;

namespace Content.Shared._RMC14.TacticalMap;

/// <summary>
/// System that grants live tactical map updates to wearers of TacticalMapGrantClothing items
/// </summary>
public sealed class TacticalMapGrantClothingSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TacticalMapGrantClothingComponent, ClothingGotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<TacticalMapGrantClothingComponent, ClothingGotUnequippedEvent>(OnGotUnequipped);
    }

    private void OnGotEquipped(Entity<TacticalMapGrantClothingComponent> clothing, ref ClothingGotEquippedEvent args)
    {
        if (!TryComp<TacticalMapUserComponent>(args.Wearer, out var tacMap))
            return;

        // Enable live updates
        tacMap.LiveUpdate = true;
        Dirty(args.Wearer, tacMap);
    }

    private void OnGotUnequipped(Entity<TacticalMapGrantClothingComponent> clothing, ref ClothingGotUnequippedEvent args)
    {
        // Check if the wearer has TacticalMapUser component
        if (!TryComp<TacticalMapUserComponent>(args.Wearer, out var tacMap))
            return;

        // Disable live updates
        tacMap.LiveUpdate = false;
        Dirty(args.Wearer, tacMap);
    }
}
