using Content.Shared._RMC14.Xenonids;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.UniformAccessories;

public abstract class SharedUniformAccessorySystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<UniformAccessoryHolderComponent, MapInitEvent>(OnHolderMapInit);
        SubscribeLocalEvent<UniformAccessoryHolderComponent, AfterAutoHandleStateEvent>(OnHolderAfterState);
        SubscribeLocalEvent<UniformAccessoryHolderComponent, EntInsertedIntoContainerMessage>(OnHolderInsertedContainer);
        SubscribeLocalEvent<UniformAccessoryHolderComponent, EntRemovedFromContainerMessage>(OnHolderRemovedContainer);
        SubscribeLocalEvent<UniformAccessoryHolderComponent, InteractUsingEvent>(OnHolderInteractUsing);
        SubscribeLocalEvent<UniformAccessoryHolderComponent, GotEquippedEvent>(OnHolderGotEquipped);
        SubscribeLocalEvent<UniformAccessoryHolderComponent, GetVerbsEvent<EquipmentVerb>>(OnHolderGetEquipmentVerbs);
    }

    private void OnHolderMapInit(Entity<UniformAccessoryHolderComponent> ent, ref MapInitEvent args)
    {
        _container.EnsureContainer<Container>(ent, ent.Comp.ContainerId);

        if (ent.Comp.StartingAccessories is not { } startingAccessories)
            return;

        foreach (var startingEntId in startingAccessories)
        {
            SpawnInContainerOrDrop(startingEntId, ent.Owner, ent.Comp.ContainerId);
        }
    }

    private void OnHolderAfterState(Entity<UniformAccessoryHolderComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        _item.VisualsChanged(ent);
    }

    private void OnHolderInsertedContainer(Entity<UniformAccessoryHolderComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        _item.VisualsChanged(ent);
    }

    private void OnHolderRemovedContainer(Entity<UniformAccessoryHolderComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        _item.VisualsChanged(ent);
    }

    private void OnHolderInteractUsing(Entity<UniformAccessoryHolderComponent> ent, ref InteractUsingEvent args)
    {
        if (!TryComp(args.Used, out UniformAccessoryComponent? accessory))
            return;

        var container = _container.EnsureContainer<Container>(ent, ent.Comp.ContainerId);
        args.Handled = true;

        if (accessory.User is { } accessoryUser && !BelongsToUser(accessoryUser, args.User))
        {
            _popup.PopupClient(Loc.GetString("rmc-uniform-accessory-fail"), args.User, args.User, PopupType.SmallCaution);
            _hands.TryDrop(args.User, ent, checkActionBlocker: false);
            return;
        }

        if (!ent.Comp.AllowedCategories.Contains(accessory.Category))
        {
            _popup.PopupClient(Loc.GetString("rmc-uniform-accessory-fail-not-allowed"), args.User, args.User, PopupType.SmallCaution);
            return;
        }

        var accessoryDictionary = new Dictionary<string, int>();

        foreach (var inserted in container.ContainedEntities)
        {
            if (TryComp<UniformAccessoryComponent>(inserted, out var insertedComp))
                accessoryDictionary[insertedComp.Category] += 1;
        }

        var limit = accessoryDictionary[accessory.Category];

        if (limit >= accessory.Limit)
        {
            _popup.PopupClient(Loc.GetString("rmc-uniform-accessory-fail-limit"), args.User, args.User, PopupType.SmallCaution);
            return;
        }

        _container.Insert(args.Used, container);
        _item.VisualsChanged(ent);
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
                    _container.Remove(ent.Owner, container);
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
                    return;
                }
            },
            IconEntity = GetNetEntity(firstAccessory),
        });
    }

    public bool BelongsToUser(NetEntity user, EntityUid target)
    {
        return user == GetNetEntity(target);
    }
}
