using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Item;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Item;

public sealed class ItemCamouflageSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly ClothingSystem _clothing = default!;
    [Dependency] private readonly SharedItemSystem _itemSystem = default!;
    [Dependency] private readonly IGameTiming _time = default!;

    [ViewVariables(VVAccess.ReadWrite)]
    public CamouflageType CurrentMapCamouflage { get; set; } = CamouflageType.Jungle;

    private readonly Queue<Entity<ItemCamouflageComponent>> _items = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<ItemCamouflageComponent, MapInitEvent>(OnMapInit);
    }

    //thank you smugleaf!
    public override void Update(float frameTime)
    {
        if (_items.Count == 0)
            return;

        foreach (var ent in _items)
        {
            if (!TryComp(ent.Owner, out MetaDataComponent? meta))
                continue;

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
        _appearance.SetData(ent.Owner, CamouflageState.Layer, type);
        
        Dirty(ent, ent.Comp);
    }
}
