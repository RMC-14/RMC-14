using Robust.Shared.Network;
using Content.Shared.Item;
using Content.Shared.Inventory;
using Robust.Shared.Containers;
using Content.Shared.Roles;
using Robust.Shared.Timing;
using Content.Shared.Weapons.Reflect;

namespace Content.Shared._RMC14.Item;

public sealed class ItemCamouflageSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly InventorySystem _inv = default!;
    [Dependency] private readonly SharedContainerSystem _cont = default!;
    [Dependency] private readonly IGameTiming _time = default!;

    [ViewVariables(VVAccess.ReadWrite)]
    public CamouflageType CurrentMapCamouflage { get; set; } = CamouflageType.Jungle;

    public Queue<Entity<ItemCamouflageComponent>> Comps = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<ItemCamouflageComponent, MapInitEvent>(OnMapInit);
    }
    //thank you smugleaf!
    public override void Update(float frameTime)
    {
        if (Comps.Count == 0)
            return;
        foreach (var ent in Comps)
        {
            if (TryComp(ent.Owner, out MetaDataComponent? meta))
            {
                if (meta.LastModifiedTick != _time.CurTick)
                {
                    Replace(ent);
                    Comps.Dequeue();
                    break;
                }
            }
        }
    }

    public void Replace(Entity<ItemCamouflageComponent> ent)
    {
        if (_net.IsClient)
            return;

        switch (CurrentMapCamouflage)
        {
            case CamouflageType.Jungle:
                ReplaceWithCamouflaged(ent, CamouflageType.Jungle);
                break;
            case CamouflageType.Desert:
                ReplaceWithCamouflaged(ent, CamouflageType.Desert);
                break;
            case CamouflageType.Snow:
                ReplaceWithCamouflaged(ent, CamouflageType.Snow);
                break;
            case CamouflageType.Classic:
                ReplaceWithCamouflaged(ent, CamouflageType.Classic);
                break;
            case CamouflageType.Urban:
                ReplaceWithCamouflaged(ent, CamouflageType.Urban);
                break;
        }
    }

    public void OnMapInit(Entity<ItemCamouflageComponent> ent, ref MapInitEvent args)
    {
        if (_net.IsClient)
            return;
        Comps.Enqueue(ent);
    }

    private void ReplaceWithCamouflaged(Entity<ItemCamouflageComponent> ent, CamouflageType type)
    {
        if (_cont.IsEntityInContainer(ent.Owner))
        {
            {
                _cont.TryGetContainingContainer((Entity<TransformComponent?, MetaDataComponent?>)ent.Owner,
                    out var cont);
                if (cont == null)
                    return;
                _cont.Remove((Entity<TransformComponent?, MetaDataComponent?>)ent.Owner, cont, true, true);
                _entityManager.SpawnInContainerOrDrop(ent.Comp.CamouflageVariations[type].Id, cont.Owner, cont.ID);
                _entityManager.QueueDeleteEntity(ent.Owner);
            }
        }
        else
        {
            _entityManager.SpawnNextToOrDrop(ent.Comp.CamouflageVariations[type], ent.Owner);
            _entityManager.QueueDeleteEntity(ent.Owner);
        }
    }
}
