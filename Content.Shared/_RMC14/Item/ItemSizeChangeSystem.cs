using Content.Shared._RMC14.Attachable.Systems;
using Content.Shared._RMC14.Explosion;
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

        SubscribeLocalEvent<ItemSizeChangeComponent, MapInitEvent>(OnItemSizeChangeMapInit,
            before: new[] { typeof(AttachableHolderSystem) });

        SubscribeLocalEvent<ChangeItemSizeOnTimerTriggerComponent, RMCActiveTimerTriggerEvent>(OnChangeItemSizeOnTimerTrigger);
        SubscribeLocalEvent<ChangeItemSizeOnTimerTriggerComponent, RMCTriggerEvent>(OnTriggered);

        InitItemSizes();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (!args.ByType.ContainsKey(typeof(ItemSizePrototype)) && args.Removed?.ContainsKey(typeof(ItemSizePrototype)) != true)
            return;

        InitItemSizes();
    }

    private void OnItemSizeChangeMapInit(Entity<ItemSizeChangeComponent> item, ref MapInitEvent args)
    {
        InitItem(item);
        RefreshItemSizeModifiers((item.Owner, item.Comp));
    }

    private void OnChangeItemSizeOnTimerTrigger(Entity<ChangeItemSizeOnTimerTriggerComponent> ent, ref RMCActiveTimerTriggerEvent args)
    {
        if (TryComp(ent, out ItemComponent? item))
        {
            ent.Comp.OriginalSize = item.Size;
            Dirty(ent);
        }

        _itemSystem.SetSize(ent, ent.Comp.Size);
    }

    private void OnTriggered(Entity<ChangeItemSizeOnTimerTriggerComponent> ent, ref RMCTriggerEvent args)
    {
        if (ent.Comp.OriginalSize == null)
            return;

        _itemSystem.SetSize(ent, ent.Comp.OriginalSize.Value);
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

    public void RefreshItemSizeModifiers(Entity<ItemSizeChangeComponent?> item)
    {
        if (item.Comp == null)
            item.Comp = EnsureComp<ItemSizeChangeComponent>(item.Owner);
        else if (!InitItem((item.Owner, item.Comp)))
            return;

        if (item.Comp == null || item.Comp.BaseSize == null)
            return;

        var ev = new GetItemSizeModifiersEvent(item.Comp.BaseSize.Value);
        RaiseLocalEvent(item.Owner, ref ev);

        ev.Size = Math.Clamp(ev.Size, 0, _sortedSizes.Count > 0 ? _sortedSizes.Count - 1 : 0);

        if (_sortedSizes.Count <= ev.Size)
            return;

        _itemSystem.SetSize(item, _sortedSizes[ev.Size]);
    }

    private bool InitItem(Entity<ItemSizeChangeComponent> item, bool onlyNull = false)
    {
        if (!onlyNull && item.Comp.BaseSize != null)
            return true;

        if (_sortedSizes.Count <= 0)
        {
            InitItemSizes();

            if (_sortedSizes.Count <= 0)
                return false;
        }

        if (!TryComp(item.Owner, out ItemComponent? itemComponent) || !_prototypeManager.TryIndex(itemComponent.Size, out ItemSizePrototype? prototype))
            return false;

        var size = _sortedSizes.IndexOf(prototype);

        if (size < 0)
            return false;

        item.Comp.BaseSize = size;
        Dirty(item);

        return true;
    }
}
