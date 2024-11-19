using Content.Shared._RMC14.Xenonids;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Medal;

public abstract class SharedPlaytimeMedalSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PlaytimeMedalUserComponent, ComponentRemove>(OnPlaytimeMedalUserRemove);
        SubscribeLocalEvent<PlaytimeMedalUserComponent, EntityTerminatingEvent>(OnPlaytimeMedalUserRemove);

        SubscribeLocalEvent<PlaytimeMedalComponent, ComponentRemove>(OnPlaytimeMedalRemove);
        SubscribeLocalEvent<PlaytimeMedalComponent, EntityTerminatingEvent>(OnPlaytimeMedalRemove);

        SubscribeLocalEvent<PlaytimeMedalHolderComponent, AfterAutoHandleStateEvent>(OnPlaytimeMedalHolderAfterState);
        SubscribeLocalEvent<PlaytimeMedalHolderComponent, EntInsertedIntoContainerMessage>(OnPlaytimeMedalHolderInsertedContainer);
        SubscribeLocalEvent<PlaytimeMedalHolderComponent, EntRemovedFromContainerMessage>(OnPlaytimeMedalHolderRemovedContainer);
        SubscribeLocalEvent<PlaytimeMedalHolderComponent, InteractUsingEvent>(OnPlaytimeMedalHolderInteractUsing);
        SubscribeLocalEvent<PlaytimeMedalHolderComponent, GotEquippedEvent>(OnPlaytimeMedalHolderGotEquipped);
        SubscribeLocalEvent<PlaytimeMedalHolderComponent, GetVerbsEvent<EquipmentVerb>>(OnPlaytimeMedalHolderGetEquipmentVerbs);
    }

    private void OnPlaytimeMedalUserRemove<T>(Entity<PlaytimeMedalUserComponent> ent, ref T args)
    {
        if (!TryComp(ent.Comp.Medal, out PlaytimeMedalComponent? medal))
            return;

        medal.User = null;
        Dirty(ent);
    }

    private void OnPlaytimeMedalRemove<T>(Entity<PlaytimeMedalComponent> ent, ref T args)
    {
        if (!TryComp(ent.Comp.User, out PlaytimeMedalUserComponent? medal))
            return;

        medal.Medal = null;
        Dirty(ent);
    }

    private void OnPlaytimeMedalHolderAfterState(Entity<PlaytimeMedalHolderComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        _item.VisualsChanged(ent);
    }

    private void OnPlaytimeMedalHolderInsertedContainer(Entity<PlaytimeMedalHolderComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.ContainerId)
            return;

        if (!HasComp<PlaytimeMedalComponent>(args.Entity))
            return;

        ent.Comp.Medal = args.Entity;
        Dirty(ent);

        _item.VisualsChanged(ent);
    }

    private void OnPlaytimeMedalHolderRemovedContainer(Entity<PlaytimeMedalHolderComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.ContainerId)
            return;

        ent.Comp.Medal = null;
        Dirty(ent);

        _item.VisualsChanged(ent);
    }

    private void OnPlaytimeMedalHolderInteractUsing(Entity<PlaytimeMedalHolderComponent> ent, ref InteractUsingEvent args)
    {
        if (!TryComp(args.Used, out PlaytimeMedalComponent? medal))
            return;

        var container = _container.EnsureContainer<ContainerSlot>(ent, ent.Comp.ContainerId);
        if (container.ContainedEntity != null)
            return;

        if (medal.User != args.User)
        {
            _hands.TryDrop(args.User, ent, checkActionBlocker: false);
            return;
        }

        _container.Insert(args.Used, container);
        _item.VisualsChanged(ent);
    }

    private void OnPlaytimeMedalHolderGotEquipped(Entity<PlaytimeMedalHolderComponent> ent, ref GotEquippedEvent args)
    {
        if (!_container.TryGetContainer(ent, ent.Comp.ContainerId, out var container) ||
            !container.ContainedEntities.TryFirstOrNull(out var medal) ||
            !TryComp(medal, out PlaytimeMedalComponent? medalComp))
        {
            return;
        }

        if (medalComp.User == args.Equipee)
            return;

        _container.EmptyContainer(container);
        _item.VisualsChanged(ent);
    }

    private void OnPlaytimeMedalHolderGetEquipmentVerbs(Entity<PlaytimeMedalHolderComponent> ent, ref GetVerbsEvent<EquipmentVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || HasComp<XenoComponent>(args.User))
            return;

        if (!_container.TryGetContainer(ent, ent.Comp.ContainerId, out var container) ||
            !container.ContainedEntities.TryFirstOrNull(out var medalId))
        {
            return;
        }

        var user = args.User;
        args.Verbs.Add(new EquipmentVerb
        {
            Text = Loc.GetString("rmc-storage-medal-remove-verb"),
            Act = () =>
            {
                if (!_container.TryGetContainer(ent, ent.Comp.ContainerId, out container) ||
                    !container.ContainedEntities.TryFirstOrNull(out medalId) ||
                    !_container.Remove(medalId.Value, container))
                {
                    return;
                }

                _hands.TryPickupAnyHand(user, medalId.Value);
            },
            IconEntity = GetNetEntity(medalId),
        });
    }
}
