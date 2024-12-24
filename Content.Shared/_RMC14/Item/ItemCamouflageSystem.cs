using System.Linq;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Item;

public sealed class ItemCamouflageSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedContainerSystem _cont = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly IGameTiming _time = default!;

    [ViewVariables(VVAccess.ReadWrite)]
    public CamouflageType CurrentMapCamouflage { get; set; } = CamouflageType.Jungle;

    private readonly Queue<Entity<ItemCamouflageComponent>> _items = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<ItemCamouflageComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<StorageComponent, ItemCamouflageEvent>(OnItemCamoflage);
    }

    //thank you smugleaf!
    public override void Update(float frameTime)
    {
        if (_items.Count == 0)
            return;

        foreach (var ent in _items.ToList())
        {
            if (!TryComp(ent.Owner, out MetaDataComponent? meta))
            {
                _items.Dequeue();
                continue;
            }

            if (meta.LastModifiedTick == _time.CurTick)
                continue;

            Replace(ent);
            _items.Dequeue();
            break;
        }
    }

    private void Replace(Entity<ItemCamouflageComponent> ent)
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

    private void OnMapInit(Entity<ItemCamouflageComponent> ent, ref MapInitEvent args)
    {
        if (_net.IsClient)
            return;

        _items.Enqueue(ent);
    }

    private void ReplaceWithCamouflaged(Entity<ItemCamouflageComponent> ent, CamouflageType type)
    {
        if (!ent.Comp.CamouflageVariations.TryGetValue(type, out var spawn))
        {
            Log.Error($"No {type} camouflage variation found for {ToPrettyString(ent)}");
            return;
        }

        EntityUid newEnt;
        if (_cont.IsEntityInContainer(ent.Owner))
        {
            _cont.TryGetContainingContainer((ent.Owner, null), out var cont);
            if (cont == null)
                return;

            _cont.Remove(ent.Owner, cont, true, true);
            newEnt = SpawnInContainerOrDrop(spawn, cont.Owner, cont.ID);
        }
        else
        {
            newEnt = SpawnNextToOrDrop(ent.Comp.CamouflageVariations[type], ent.Owner);
        }

        var ev = new ItemCamouflageEvent(ent, newEnt, ent.Comp.OverrideStorageReplace);
        RaiseLocalEvent(ent, ref ev);

        QueueDel(ent.Owner);
    }

    private void OnItemCamoflage(Entity<StorageComponent> ent, ref ItemCamouflageEvent args)
    {
        if (args.ReplaceOverride)
            return;

        var oldItem = args.Old;
        var newItem = args.New;

        _storage.TransferEntities(oldItem, newItem);
    }
}
