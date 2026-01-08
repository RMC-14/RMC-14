using System.Linq;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using static Content.Shared._RMC14.ArmorWebbing.ArmorWebbingTransferComponent;

namespace Content.Shared._RMC14.ArmorWebbing;

public abstract class SharedArmorWebbingSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly MobStateSystem _mob = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ArmorWebbingClothingComponent, MapInitEvent>(OnArmorWebbingClothingMapInit);
        SubscribeLocalEvent<ArmorWebbingClothingComponent, InteractUsingEvent>(OnArmorWebbingClothingInteractUsing);
        SubscribeLocalEvent<ArmorWebbingClothingComponent, InventoryRelayedEvent<GetVerbsEvent<EquipmentVerb>>>(GetRelayedVerbs);
        SubscribeLocalEvent<ArmorWebbingClothingComponent, GetVerbsEvent<EquipmentVerb>>(OnArmorWebbingClothingGetEquipmentVerbs);
        SubscribeLocalEvent<ArmorWebbingClothingComponent, GetVerbsEvent<InteractionVerb>>(OnArmorWebbingClothingGetInteractionVerbs);
        SubscribeLocalEvent<ArmorWebbingClothingComponent, EntInsertedIntoContainerMessage>(OnClothingInserted);
        SubscribeLocalEvent<ArmorWebbingClothingComponent, EntRemovedFromContainerMessage>(OnClothingRemoved);
        SubscribeLocalEvent<ArmorWebbingClothingComponent, BeingEquippedAttemptEvent>(OnClothingBeingEquippedAttempt);
    }

    private void OnArmorWebbingClothingMapInit(Entity<ArmorWebbingClothingComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.StartingArmorWebbing is not { } starting)
            return;

        var armorWebbing = Spawn(starting, MapCoordinates.Nullspace);
        Attach(ent, armorWebbing, null, out _);
    }

    private void OnArmorWebbingClothingInteractUsing(Entity<ArmorWebbingClothingComponent> clothing, ref InteractUsingEvent args)
    {
        Attach(clothing, args.Used, args.User, out var handled);
        args.Handled = handled;
    }

    private void OnArmorWebbingClothingGetInteractionVerbs(Entity<ArmorWebbingClothingComponent> clothing, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || HasComp<XenoComponent>(args.User))
            return;

        if (!HasArmorWebbing((clothing, clothing), out _))
            return;

        var user = args.User;
        args.Verbs.Add(new InteractionVerb
        {
            Text = Loc.GetString("rmc-storage-webbing-remove-verb"),
            Act = () => Detach(clothing, user),
            IconEntity = GetNetEntity(clothing.Owner)
        });
    }

    private void GetRelayedVerbs(EntityUid uid, ArmorWebbingClothingComponent component, InventoryRelayedEvent<GetVerbsEvent<EquipmentVerb>> args)
    {
        OnArmorWebbingClothingGetEquipmentVerbs((uid, component), ref args.Args);
    }

    private void OnArmorWebbingClothingGetEquipmentVerbs(Entity<ArmorWebbingClothingComponent> clothing, ref GetVerbsEvent<EquipmentVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || HasComp<XenoComponent>(args.User))
            return;

        if (!HasArmorWebbing((clothing, clothing), out _))
            return;

        var wearer = Transform(clothing).ParentUid;
        var user = args.User;

        // To avoid duplicate verbs
        if (user == wearer)
            return;

        // To prevent stripping webbing from alive players
        if (!_mob.IsDead(wearer))
            return;

        args.Verbs.Add(new EquipmentVerb
        {
            Text = Loc.GetString("rmc-storage-webbing-remove-verb"),
            Act = () => Detach(clothing, user),
            IconEntity = GetNetEntity(clothing.Owner)
        });
    }

    public bool HasArmorWebbing(Entity<ArmorWebbingClothingComponent?> clothing, out Entity<ArmorWebbingComponent> armorWebbing)
    {
        armorWebbing = default;
        if (!Resolve(clothing, ref clothing.Comp, false))
            return false;

        if (!_container.TryGetContainer(clothing, clothing.Comp.Container, out var container) ||
            container.Count <= 0)
        {
            return false;
        }

        var ent = container.ContainedEntities[0];
        if (!TryComp(ent, out ArmorWebbingComponent? armorWebbingComp))
        {
            return false;
        }

        armorWebbing = (ent, armorWebbingComp);
        return true;
    }

    protected virtual void OnClothingInserted(Entity<ArmorWebbingClothingComponent> clothing, ref EntInsertedIntoContainerMessage args)
    {
        if (clothing.Comp.Container != args.Container.ID)
            return;

        clothing.Comp.ArmorWebbing = args.Entity;
        Dirty(clothing);
        _item.VisualsChanged(clothing);
    }

    protected virtual void OnClothingRemoved(Entity<ArmorWebbingClothingComponent> clothing, ref EntRemovedFromContainerMessage args)
    {
        if (clothing.Comp.Container != args.Container.ID)
            return;

        clothing.Comp.ArmorWebbing = null;
        Dirty(clothing);
        _item.VisualsChanged(clothing);
    }

    private void OnClothingBeingEquippedAttempt(Entity<ArmorWebbingClothingComponent> ent, ref BeingEquippedAttemptEvent args)
    {
        if (ent.Comp.ArmorWebbing == null)
            return;

        var slots = _inventory.GetSlotEnumerator(args.EquipTarget);
    }

    public bool Attach(Entity<ArmorWebbingClothingComponent> clothing, EntityUid armorWebbing, EntityUid? user, out bool handled)
    {
        handled = false;
        if (!TryComp(armorWebbing, out ArmorWebbingComponent? armorWebbingComp) ||
            HasComp<StorageComponent>(clothing) ||
            !HasComp<StorageComponent>(armorWebbing) ||
            !TryComp(clothing, out ItemComponent? clothingItem) ||
            !TryComp(armorWebbing, out ItemComponent? armorWebbingItem))
        {
            return false;
        }

        if (_container.TryGetContainingContainer((clothing, null), out var containing))
        {
            if (TryComp(containing.Owner, out StorageComponent? storage) &&
                storage.StoredItems.ContainsKey(clothing))
            {
                handled = true;

                if (user != null)
                    _popup.PopupClient(Loc.GetString("rmc-webbing-cannot-in-storage"), user, PopupType.LargeCaution);

                return false;
            }

            if (TryComp(containing.Owner, out InventoryComponent? inventory))
            {
                var slots = _inventory.GetSlotEnumerator((containing.Owner, inventory));
            }
        }

        var container = _container.EnsureContainer<ContainerSlot>(clothing, clothing.Comp.Container);
        if (container.Count > 0 || !_container.Insert(armorWebbing, container))
            return false;

        EntityManager.AddComponents(clothing, armorWebbingComp.Components);

        var comp = EnsureComp<ArmorWebbingTransferComponent>(armorWebbing);
        comp.Clothing = clothing;
        comp.Transfer = TransferType.ToClothing;
        Dirty(armorWebbing, comp);

        handled = true;
        return true;
    }

    private void Detach(Entity<ArmorWebbingClothingComponent> clothing, EntityUid user)
    {
        if (TerminatingOrDeleted(clothing) || !clothing.Comp.Running)
            return;

        if (!HasArmorWebbing((clothing, clothing), out var armorWebbing))
            return;

        _container.TryRemoveFromContainer(armorWebbing.Owner);
        _hands.TryPickupAnyHand(user, armorWebbing);

        EntityManager.AddComponents(armorWebbing, armorWebbing.Comp.Components);

        var comp = EnsureComp<ArmorWebbingTransferComponent>(armorWebbing);
        comp.Clothing = clothing;
        comp.Transfer = TransferType.ToArmorWebbing;
        Dirty(armorWebbing, comp);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<ArmorWebbingTransferComponent, ArmorWebbingComponent>();
        while (query.MoveNext(out var uid, out var transfer, out var armorWebbing))
        {
            // TODO RMC14 remove this once the bug with transferring on same tick upstream is fixed
            if (transfer.Defer)
            {
                transfer.Defer = false;
                continue;
            }

            RemCompDeferred<ArmorWebbingTransferComponent>(uid);

            switch (transfer.Transfer)
            {
                case TransferType.ToClothing:
                {
                    if (!TryComp(uid, out StorageComponent? storage) ||
                        transfer.Clothing is not { } clothing)
                    {
                        continue;
                    }

                    foreach (var stored in storage.Container.ContainedEntities.ToArray())
                    {
                        _storage.Insert(clothing, stored, out _, playSound: false);
                    }

                    break;
                }
                case TransferType.ToArmorWebbing:
                {
                    if (transfer.Clothing is not { } clothing)
                        continue;

                    if (TryComp(clothing, out StorageComponent? storage))
                    {
                        foreach (var stored in storage.Container.ContainedEntities.ToArray())
                        {
                            _storage.Insert(uid, stored, out _, playSound: false);
                        }
                    }

                    foreach (var entry in armorWebbing.Components.Values)
                    {
                        RemComp(clothing, entry.Component.GetType());
                    }

                    break;
                }
            }
        }
    }
}
