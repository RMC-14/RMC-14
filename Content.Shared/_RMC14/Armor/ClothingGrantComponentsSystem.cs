using Content.Shared.Inventory.Events;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Armor;

public sealed class ClothingGrantComponentsSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ClothingGrantComponentsComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<ClothingGrantComponentsComponent, GotUnequippedEvent>(OnUnequipped);
    }

    private void OnEquipped(Entity<ClothingGrantComponentsComponent> ent, ref GotEquippedEvent args)
    {
        if (!_net.IsServer)
            return;

        EntityManager.AddComponents(args.Equipee, ent.Comp.Components);
    }

    private void OnUnequipped(Entity<ClothingGrantComponentsComponent> ent, ref GotUnequippedEvent args)
    {
        if (!_net.IsServer)
            return;

        EntityManager.RemoveComponents(args.Equipee, ent.Comp.Components);
    }
}
