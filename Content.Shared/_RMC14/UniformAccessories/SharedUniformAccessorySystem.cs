using Content.Shared._RMC14.Xenonids;
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.UniformAccessories;

public abstract class SharedUniformAccessorySystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<HandsComponent, StartingGearEquippedEvent>(OnStartingGearEquipped);

        SubscribeLocalEvent<UniformAccessoryHolderComponent, MapInitEvent>(OnHolderMapInit);
        SubscribeLocalEvent<UniformAccessoryHolderComponent, InteractUsingEvent>(OnHolderInteractUsing);
        SubscribeLocalEvent<UniformAccessoryHolderComponent, GotEquippedEvent>(OnHolderGotEquipped);
        SubscribeLocalEvent<UniformAccessoryHolderComponent, GetVerbsEvent<EquipmentVerb>>(OnHolderGetEquipmentVerbs);

        SubscribeLocalEvent<UniformAccessoryComponent, ExaminedEvent>(OnAccessoryExamined);

        Subs.BuiEvents<UniformAccessoryHolderComponent>(UniformAccessoriesUi.Key,
            subs =>
            {
                subs.Event<UniformAccessoriesBuiMsg>(OnAccessoriesBuiMsg);
            });
    }

    private void OnStartingGearEquipped(Entity<HandsComponent> ent, ref StartingGearEquippedEvent args)
    {
        TryInsertInhandAccessories(ent.Owner);
    }

    private void OnHolderMapInit(Entity<UniformAccessoryHolderComponent> ent, ref MapInitEvent args)
    {
        _container.EnsureContainer<Container>(ent, ent.Comp.ContainerId);

        if (ent.Comp.StartingAccessories is not { } startingAccessories)
            return;

        if (_net.IsClient)
            return;

        foreach (var startingEntId in startingAccessories)
        {
            SpawnInContainerOrDrop(startingEntId, ent.Owner, ent.Comp.ContainerId);
        }
    }

    private void OnHolderInteractUsing(Entity<UniformAccessoryHolderComponent> ent, ref InteractUsingEvent args)
    {
        if (!HasComp<UniformAccessoryComponent>(args.Used))
            return;

        args.Handled = true;
        TryInsertUniformAccessory(args.Used, ent, args.User);
    }

    private void OnHolderGotEquipped(Entity<UniformAccessoryHolderComponent> ent, ref GotEquippedEvent args)
    {
        if (!_container.TryGetContainer(ent, ent.Comp.ContainerId, out var container))
            return;

        foreach (var accessory in container.ContainedEntities)
        {
            if (TryComp<UniformAccessoryComponent>(accessory, out var accessoryComp))
            {
                if (accessoryComp.User is { } acccessoryUser && !BelongsToUser(acccessoryUser, args.Equipee))
                {
                    _container.Remove(accessory, container);
                    return;
                }
            }
        }

        _item.VisualsChanged(ent);
    }

    private void OnHolderGetEquipmentVerbs(Entity<UniformAccessoryHolderComponent> ent, ref GetVerbsEvent<EquipmentVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || HasComp<XenoComponent>(args.User))
            return;

        if (!_container.TryGetContainer(ent, ent.Comp.ContainerId, out var container) ||
            !container.ContainedEntities.TryFirstOrNull(out var firstAccessory))
        {
            return;
        }

        var user = args.User;
        args.Verbs.Add(new EquipmentVerb
        {
            Text = Loc.GetString("rmc-uniform-accessory-remove"),
            Act = () =>
            {
                // only one accessory, don't bother with the UI
                if (container.ContainedEntities.Count == 1 && firstAccessory != null)
                {
                    _container.Remove(firstAccessory.Value, container);
                    _hands.TryPickupAnyHand(user, firstAccessory.Value);
                    _item.VisualsChanged(ent);
                    return;
                }

                // otherwise open the UI
                _ui.OpenUi(ent.Owner, UniformAccessoriesUi.Key, user);
            },
            IconEntity = GetNetEntity(firstAccessory),
        });
    }

    private void OnAccessoriesBuiMsg(Entity<UniformAccessoryHolderComponent> ent, ref UniformAccessoriesBuiMsg args)
    {
        var user = args.Actor;
        var toRemove = GetEntity(args.ToRemove);

        if (!_container.TryGetContainer(ent, ent.Comp.ContainerId, out var container))
            return;

        if (_container.Remove(toRemove, container))
        {
            _hands.TryPickupAnyHand(user, toRemove);
            _item.VisualsChanged(ent);
        }

        if (container.ContainedEntities.Count <= 1)
        {
            _ui.CloseUi(ent.Owner, UniformAccessoriesUi.Key);
        }
        else
        {
            var state = new UniformAccessoriesBuiState();
            _ui.SetUiState(ent.Owner, UniformAccessoriesUi.Key, state);
        }
    }

    private void OnAccessoryExamined(Entity<UniformAccessoryComponent> ent, ref ExaminedEvent args)
    {
        if (HasComp<XenoComponent>(args.Examiner))
            return;

        if (ent.Comp.User == null)
            return;

        if (!TryGetEntity(ent.Comp.User, out var owner))
            return;

        var ownerName = Name(owner.Value);
        args.PushMarkup(Loc.GetString("rmc-uniform-accessory-owner", ("owner", ownerName)));
    }

    public void TryInsertInhandAccessories(EntityUid target)
    {
        foreach (var held in _hands.EnumerateHeld(target))
        {
            if (!HasComp<UniformAccessoryComponent>(held))
                continue;

            TryInsertToValidSlot(held, target);
        }
    }

    public bool TryInsertToValidSlot(EntityUid accessory, EntityUid user)
    {
        var slots = _inventory.GetSlotEnumerator(user, SlotFlags.INNERCLOTHING | SlotFlags.OUTERCLOTHING);
        while (slots.MoveNext(out var slot))
        {
            if (slot.ContainedEntity == null || !HasComp<UniformAccessoryHolderComponent>(slot.ContainedEntity))
                continue;

            if (TryInsertUniformAccessory(accessory, slot.ContainedEntity.Value, user))
                return true;
        }

        return false;
    }

    public bool TryInsertUniformAccessory(EntityUid accessory, EntityUid holder, EntityUid user)
    {
        if (!TryComp(accessory, out UniformAccessoryComponent? accessoryComp))
            return false;

        if (!TryComp(holder, out UniformAccessoryHolderComponent? holderComp))
            return false;

        var container = _container.EnsureContainer<Container>(holder, holderComp.ContainerId);

        if (accessoryComp.User is { } accessoryUser && !BelongsToUser(accessoryUser, user))
        {
            _popup.PopupClient(Loc.GetString("rmc-uniform-accessory-fail"), user, user, PopupType.SmallCaution);
            return false;
        }

        if (!holderComp.AllowedCategories.Contains(accessoryComp.Category))
        {
            _popup.PopupClient(Loc.GetString("rmc-uniform-accessory-fail-not-allowed"), user, user, PopupType.SmallCaution);
            return false;
        }

        var accessoryDictionary = new Dictionary<string, int>();

        foreach (var inserted in container.ContainedEntities)
        {
            if (TryComp<UniformAccessoryComponent>(inserted, out var insertedComp))
            {
                if (accessoryDictionary.TryGetValue(insertedComp.Category, out var count))
                    accessoryDictionary[insertedComp.Category] = count + 1;
                else
                    accessoryDictionary[insertedComp.Category] = 1;
            }
        }

        if (accessoryDictionary.TryGetValue(accessoryComp.Category, out var amount) && accessoryComp.Limit <= amount)
        {
            _popup.PopupClient(Loc.GetString("rmc-uniform-accessory-fail-limit"), user, user, PopupType.SmallCaution);
            return false;
        }

        _container.Insert(accessory, container);
        _item.VisualsChanged(holder);
        return true;
    }

    public bool BelongsToUser(NetEntity user, EntityUid target)
    {
        return user == GetNetEntity(target);
    }

    public void SetAccessoriesHidden(EntityUid accessoryHolder, bool hideAccessories)
    {
        if (!TryComp<UniformAccessoryHolderComponent>(accessoryHolder, out var comp))
            return;

        comp.HideAccessories = hideAccessories;
        Dirty(accessoryHolder, comp);

        _item.VisualsChanged(accessoryHolder);
    }
}
