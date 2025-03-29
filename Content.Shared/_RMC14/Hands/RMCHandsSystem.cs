using Content.Shared._RMC14.Storage;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;

namespace Content.Shared._RMC14.Hands;

public sealed class RMCHandsSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly RMCStorageSystem _rmcStorage = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GiveHandsComponent, MapInitEvent>(OnXenoHandsMapInit);
        SubscribeLocalEvent<WhitelistPickupByComponent, GettingPickedUpAttemptEvent>(OnWhitelistGettingPickedUpAttempt);
        SubscribeLocalEvent<WhitelistPickupComponent, PickupAttemptEvent>(OnWhitelistPickUpAttempt);
        SubscribeLocalEvent<DropHeldOnIncapacitateComponent, MobStateChangedEvent>(OnDropMobStateChanged);
        SubscribeLocalEvent<RMCStorageEjectHandComponent, GetVerbsEvent<AlternativeVerb>>(OnStorageEjectHandVerbs);
        SubscribeLocalEvent<DropOnUseInHandComponent, UseInHandEvent>(OnDropOnUseInHand);
    }

    private void OnXenoHandsMapInit(Entity<GiveHandsComponent> ent, ref MapInitEvent args)
    {
        foreach (var hand in ent.Comp.Hands)
        {
            _hands.AddHand(ent, hand.Name, hand.Location);
        }
    }

    private void OnWhitelistGettingPickedUpAttempt(Entity<WhitelistPickupByComponent> ent, ref GettingPickedUpAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!_whitelist.IsValid(ent.Comp.Whitelist, args.User))
            args.Cancel();
    }

    private void OnWhitelistPickUpAttempt(Entity<WhitelistPickupComponent> ent, ref PickupAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!_whitelist.IsValid(ent.Comp.Whitelist, args.Item))
            args.Cancel();
    }

    private void OnDropMobStateChanged(Entity<DropHeldOnIncapacitateComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.OldMobState != MobState.Alive ||
            args.NewMobState <= MobState.Alive)
        {
            return;
        }

        if (!TryComp(ent, out HandsComponent? handsComp))
            return;

        foreach (var hand in handsComp.Hands.Values)
        {
            _hands.TryDrop(ent, hand, checkActionBlocker: false, handsComp: handsComp);
        }
    }

    private void OnStorageEjectHandVerbs(Entity<RMCStorageEjectHandComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract)
            return;

        if (!_inventory.TryGetContainingSlot(ent.Owner, out var slot))
            return;

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb
        {
            Text = "Unequip",
            Act = () =>
            {
                if (_inventory.TryGetContainingSlot(ent.Owner, out slot) &&
                    _inventory.TryUnequip(user, user, slot.Name, checkDoafter: true))
                {
                    _hands.TryPickupAnyHand(user, ent.Owner);
                }
            },
        });
    }

    private void OnDropOnUseInHand(Entity<DropOnUseInHandComponent> ent, ref UseInHandEvent args)
    {
        _hands.TryDrop(args.User, ent);
    }

    public bool IsPickupByAllowed(Entity<WhitelistPickupByComponent?> item, Entity<WhitelistPickupComponent?> user)
    {
        Resolve(item, ref item.Comp, false);
        Resolve(user, ref user.Comp, false);

        if (item.Comp != null && !_whitelist.IsValid(item.Comp.Whitelist, user))
            return false;

        if (user.Comp != null && !_whitelist.IsValid(user.Comp.Whitelist, item.Owner))
            return false;

        return true;
    }

    public bool TryGetHolder(EntityUid item, out EntityUid user)
    {
        user = default;
        if (!_container.TryGetContainingContainer((item, null), out var container))
            return false;

        if (!_hands.IsHolding(container.Owner, item))
            return false;

        user = container.Owner;
        return true;
    }

    public bool TryStorageEjectHand(EntityUid user, string handName)
    {
        if (!_hands.TryGetHand(user, handName, out var hand) ||
            hand.HeldEntity is not { } held)
        {
            return false;
        }

        if (HasComp<InteractionActivateOnClickComponent>(held) &&
            _interaction.InteractionActivate(user, held))
        {
            return true;
        }

        return TryStorageEjectHand(user, held);
    }

    public bool TryStorageEjectHand(EntityUid user, EntityUid item)
    {
        if (!TryComp(item, out RMCStorageEjectHandComponent? eject) ||
            !TryComp(item, out StorageComponent? storage))
        {
            return false;
        }

        if (eject.Whitelist != null)
        {
            foreach (var contained in storage.Container.ContainedEntities)
            {
                if (_whitelist.IsWhitelistPass(eject.Whitelist, contained))
                {
                    _hands.TryPickupAnyHand(user, contained);
                    return true;
                }
            }
        }

        if (!_rmcStorage.TryGetLastItem((item, storage), out var last))
        {
            _popup.PopupClient(Loc.GetString("rmc-storage-nothing-left", ("storage", item)), user, user);
            return true;
        }

        _hands.TryPickupAnyHand(user, last);
        return true;
    }
}
