using Content.Shared._RMC14.Attachable.Systems;
using Content.Shared.Item;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Item;

public sealed partial class ItemSizeChangeSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedItemSystem _itemSystem = default!;

    private readonly List<ItemSizePrototype> _sortedSizes = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        SubscribeLocalEvent<ItemSizeChangeComponent, MapInitEvent>(OnMapInit,
            before: new[] { typeof(AttachableHolderSystem) });

        InitItemSizes();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (!args.ByType.ContainsKey(typeof(ItemSizePrototype)) && args.Removed?.ContainsKey(typeof(ItemSizePrototype)) != true)
            return;

        InitItemSizes();
    }

    private void InitItemSizes()
    {
        _sortedSizes.Clear();

        foreach (var prototype in _prototypeManager.EnumeratePrototypes<ItemSizePrototype>())
        {
            if (prototype.ID.Equals("Invalid"))
                continue;

            _sortedSizes.Add(prototype);
        }

        _sortedSizes.Sort();
    }

    private void OnMapInit(Entity<ItemSizeChangeComponent> item, ref MapInitEvent args)
    {
        InitItem(item);
    }

    public void RefreshItemSizeModifiers(Entity<ItemSizeChangeComponent?> item)
    {
        if (item.Comp == null)
        {
            if (!TryComp(item.Owner, out ItemSizeChangeComponent? sizeChangeComponent))
            {
                item.Comp = EnsureComp<ItemSizeChangeComponent>(item.Owner);
                InitItem((item.Owner, item.Comp));
            }
            else
                item.Comp = sizeChangeComponent;
        }

        if (!TryComp(item.Owner, out ItemComponent? itemComponent))
            return;

        var ev = new GetItemSizeModifiersEvent(item.Comp.BaseSize);
        RaiseLocalEvent(item.Owner, ref ev);

        ev.Size = Math.Clamp(ev.Size, 0, _sortedSizes.Count > 0 ? _sortedSizes.Count - 1 : 0);

        if (_sortedSizes.Count <= ev.Size)
            return;

        _itemSystem.SetSize(item, _sortedSizes[ev.Size], itemComponent);
        Dirty(item);
    }

    private void InitItem(Entity<ItemSizeChangeComponent> item)
    {
        if (!TryComp(item.Owner, out ItemComponent? itemComponent) || !_prototypeManager.TryIndex(itemComponent.Size, out ItemSizePrototype? prototype))
            return;

        item.Comp.BaseSize = _sortedSizes.IndexOf(prototype);
    }
}
