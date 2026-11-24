using Content.Shared._RMC14.Inventory;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Examine;
using Content.Shared.Hands;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Armor.Magnetic;

public sealed class RMCMagneticSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ItemSlotsSystem _slots = default!;
    [Dependency] private readonly ThrownItemSystem _thrownItem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCMagneticItemComponent, DroppedEvent>(OnMagneticItemDropped);
        SubscribeLocalEvent<RMCMagneticItemComponent, RMCDroppedEvent>(OnMagneticItemRMCDropped);
        SubscribeLocalEvent<RMCMagneticItemComponent, ThrownEvent>(OnMagneticItemThrown);
        SubscribeLocalEvent<RMCMagneticItemComponent, DropAttemptEvent>(OnMagneticItemDropAttempt);

        SubscribeLocalEvent<RMCMagneticArmorComponent, InventoryRelayedEvent<RMCMagnetizeItemEvent>>(OnMagnetizeItem);

        SubscribeLocalEvent<InventoryComponent, RMCMagnetizeItemEvent>(_inventory.RelayEvent);

        SubscribeLocalEvent<RMCSlingPouchComponent, EntInsertedIntoContainerMessage>(OnSlingStore);
        SubscribeLocalEvent<RMCSlingPouchComponent, GetVerbsEvent<AlternativeVerb>>(OnSlingGetVerbs);
        SubscribeLocalEvent<RMCSlingPouchComponent, InventoryRelayedEvent<RMCMagnetizeItemEvent>>(OnSlingDrop);

        SubscribeLocalEvent<RMCSlingPouchItemComponent, GetVerbsEvent<AlternativeVerb>>(OnSlingItemGetVerbs);
        SubscribeLocalEvent<RMCSlingPouchItemComponent, ExaminedEvent>(OnSlingItemExamine);
    }

    private void OnMagneticItemDropped(Entity<RMCMagneticItemComponent> ent, ref DroppedEvent args)
    {
        TryReturn(ent, args.User);
    }

    private void OnMagneticItemRMCDropped(Entity<RMCMagneticItemComponent> ent, ref RMCDroppedEvent args)
    {
        TryReturn(ent, args.User);
    }

    private void OnMagneticItemThrown(Entity<RMCMagneticItemComponent> ent, ref ThrownEvent args)
    {
        if (args.User is not { } user)
            return;

        if (!TryReturn(ent, user))
            return;

        if (TryComp(ent, out ThrownItemComponent? thrown))
            _thrownItem.StopThrow(ent, thrown);
    }

    private void OnMagneticItemDropAttempt(Entity<RMCMagneticItemComponent> ent, ref DropAttemptEvent args)
    {
        if (!CanReturn(ent, args.Uid, out _, out _, out _))
            return;

        args.Cancel();
    }

    private void OnMagnetizeItem(Entity<RMCMagneticArmorComponent> ent, ref InventoryRelayedEvent<RMCMagnetizeItemEvent> args)
    {
        var everySlotEnumerator = _inventory.GetSlotEnumerator(args.Args.User);
        while (everySlotEnumerator.MoveNext(out var slot))
        {
            if (slot.ContainedEntity is not { } slotItem ||
                !HasComp<RMCMagneticItemReceiverComponent>(slotItem) ||
                !TryComp<ItemSlotsComponent>(slotItem, out var itemSlotsComp))
                continue;

            foreach (var slotContainer in _container.GetAllContainers(slotItem))
            {
                if (!_slots.TryGetSlot(slotItem, slotContainer.ID, out var itemSlot, itemSlotsComp))
                    continue;

                if (!_slots.CanInsert(ent, args.Args.Item, args.Args.User, itemSlot, false))
                    continue;

                args.Args.Magnetizer = ent;
                args.Args.ReceivingItem = slotItem;
                args.Args.ReceivingContainer = slotContainer.ID;
                return;
            }
        }

        if ((ent.Comp.AllowMagnetizeToSlots & args.Args.MagnetizeToSlots) == SlotFlags.NONE)
            return;

        var slotEnumerator = _inventory.GetSlotEnumerator(args.Args.User, ent.Comp.AllowMagnetizeToSlots & args.Args.MagnetizeToSlots);

        while (slotEnumerator.MoveNext(out var container))
        {
            if (container.Count > 0)
                continue;

            args.Args.Magnetizer = ent;
            break;
        }
    }

    private bool CanReturn(Entity<RMCMagneticItemComponent> ent, EntityUid user, out EntityUid magnetizer, out EntityUid? receivingItem, out string receivingContainer)
    {
        var ev = new RMCMagnetizeItemEvent(user, ent.Owner, ent.Comp.MagnetizeToSlots, SlotFlags.OUTERCLOTHING | SlotFlags.POCKET);
        RaiseLocalEvent(user, ref ev);

        magnetizer = ev.Magnetizer ?? default;
        receivingItem = ev.ReceivingItem;
        receivingContainer = ev.ReceivingContainer;
        return magnetizer != default;
    }

    private bool TryReturn(Entity<RMCMagneticItemComponent> ent, EntityUid user)
    {
        if (!CanReturn(ent, user, out var magnetizer, out var receivingItem, out var receivingContainer))
            return false;

        var returnComp = EnsureComp<RMCReturnToInventoryComponent>(ent);
        returnComp.User = user;
        returnComp.Magnetizer = magnetizer;
        returnComp.ReceivingItem = receivingItem;
        returnComp.ReceivingContainer = receivingContainer;

        Dirty(ent, returnComp);
        return true;
    }

    public void SetMagnetizeToSlots(Entity<RMCMagneticItemComponent> ent, SlotFlags slots)
    {
        ent.Comp.MagnetizeToSlots = slots;
        Dirty(ent);
    }

    public void OnSlingDrop(Entity<RMCSlingPouchComponent> pouch, ref InventoryRelayedEvent<RMCMagnetizeItemEvent> args)
    {
        var item = args.Args.Item;
        if (pouch.Comp.Item != item)
            return;

        foreach (var slotContainer in _container.GetAllContainers(pouch))
        {
            if (!_container.CanInsert(item, slotContainer))
                continue;

            args.Args.Magnetizer = pouch;
            args.Args.ReceivingItem = pouch;
            args.Args.ReceivingContainer = slotContainer.ID;
            return;
        }
    }

    public void OnSlingStore(Entity<RMCSlingPouchComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        // Skip if same item
        if (ent.Comp.Item == args.Entity)
            return;

        var popup = Loc.GetString("rmc-sling-link", ("item", args.Entity), ("pouch", ent.Owner));
        _popup.PopupClient(popup, args.OldParent, args.OldParent, PopupType.Medium);

        if (_net.IsClient)
            return;

        // Unlink
        if (ent.Comp.Item is { } oldItem)
        {
            RemComp<RMCMagneticItemComponent>(oldItem);
            RemComp<RMCSlingPouchItemComponent>(oldItem);
        }

        if (TryComp<RMCSlingPouchItemComponent>(args.Entity, out var oldSling) &&
            TryComp<RMCSlingPouchComponent>(oldSling.Pouch, out var oldSlingComp))
        {
            oldSlingComp.Item = null;
            Dirty(oldSling.Pouch, oldSlingComp);
        }

        // Link
        AddComp(args.Entity, new RMCMagneticItemComponent
        {
            MagnetizeToSlots = SlotFlags.NONE,
        }, true);
        AddComp(args.Entity, new RMCSlingPouchItemComponent
        {
            Pouch = ent.Owner,
        }, true);

        ent.Comp.Item = args.Entity;
        Dirty(ent);
    }

    public void OnSlingGetVerbs(Entity<RMCSlingPouchComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (ent.Comp.Item is not { } item)
            return;

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("rmc-sling-unlink-verb-sling"),
            Act = () =>
            {
                RemComp<RMCMagneticItemComponent>(item);
                RemComp<RMCSlingPouchItemComponent>(item);

                ent.Comp.Item = null;
                Dirty(ent);

                var popup = Loc.GetString("rmc-sling-unlink", ("item", item), ("pouch", ent.Owner));
                _popup.PopupClient(popup, user, user, PopupType.Medium);
            }
        });
    }

    public void OnSlingItemGetVerbs(Entity<RMCSlingPouchItemComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;
        var pouch = ent.Comp.Pouch;
        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("rmc-sling-unlink-verb-item"),
            Act = () =>
            {
                if (TryComp<RMCSlingPouchComponent>(pouch, out var pouchComp))
                {
                    pouchComp.Item = null;
                    Dirty(pouch, pouchComp);
                }

                RemComp<RMCMagneticItemComponent>(ent);
                RemComp<RMCSlingPouchItemComponent>(ent);

                var popup = Loc.GetString("rmc-sling-unlink", ("item", ent.Owner), ("pouch", pouch));
                _popup.PopupClient(popup, user, user, PopupType.Medium);
            }
        });
    }

    public void OnSlingItemExamine(Entity<RMCSlingPouchItemComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("rmc-sling-attached", ("pouch", ent.Comp.Pouch)));
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<RMCReturnToInventoryComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Returned)
                continue;

            var user = comp.User;
            var magnetizer = comp.Magnetizer;
            if (!TerminatingOrDeleted(user) && !TerminatingOrDeleted(magnetizer))
            {
                if (comp.ReceivingItem is { } insertInto)
                {
                    if (_container.TryGetContainer(insertInto, comp.ReceivingContainer, out var container) &&
                        _container.Insert(uid, container, force: true))
                    {
                        var popup = Loc.GetString("rmc-magnetize-return",
                            ("item", uid),
                            ("magnetizer", insertInto));
                        _popup.PopupClient(popup, user, user, PopupType.Medium);

                        comp.Returned = true;
                        Dirty(uid, comp);
                    }
                }
                else
                {
                    var slots = _inventory.GetSlotEnumerator(user, SlotFlags.SUITSTORAGE);
                    while (slots.MoveNext(out var slot))
                    {
                        if (_inventory.TryEquip(user, uid, slot.ID, force: true))
                        {
                            var popup = Loc.GetString("rmc-magnetize-return",
                                ("item", uid),
                                ("magnetizer", magnetizer));
                            _popup.PopupClient(popup, user, user, PopupType.Medium);

                            comp.Returned = true;
                            Dirty(uid, comp);
                            break;
                        }
                    }
                }
            }

            RemCompDeferred<RMCReturnToInventoryComponent>(uid);
        }
    }
}
