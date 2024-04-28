using Content.Shared.Clothing.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._CM14.Vendors;

public abstract class SharedCMAutomatedVendorSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    private readonly SlotFlags[] _order =
    [
        SlotFlags.BACK,
        SlotFlags.IDCARD,
        SlotFlags.INNERCLOTHING,
        SlotFlags.OUTERCLOTHING,
        SlotFlags.HEAD,
        SlotFlags.FEET,
        SlotFlags.MASK,
        SlotFlags.GLOVES,
        SlotFlags.EARS,
        SlotFlags.EYES,
        SlotFlags.BELT,
        SlotFlags.SUITSTORAGE,
        SlotFlags.NECK,
        SlotFlags.POCKET,
        SlotFlags.LEGS
    ];

    public override void Initialize()
    {
        Subs.BuiEvents<CMAutomatedVendorComponent>(CMAutomatedVendorUI.Key, subs =>
        {
            subs.Event<CMVendorVendBuiMessage>(OnVendBui);
        });
    }

    protected virtual void OnVendBui(Entity<CMAutomatedVendorComponent> vendor, ref CMVendorVendBuiMessage args)
    {
        var sections = vendor.Comp.Sections.Count;
        var playerName = args.Session.Name;
        if (args.Section < 0 || args.Section >= sections)
        {
            Log.Error($"Player {playerName} sent an invalid vend section: {args.Section}. Max: {sections}");
            return;
        }

        var section = vendor.Comp.Sections[args.Section];
        var entries = section.Entries.Count;
        if (args.Entry < 0 || args.Entry >= entries)
        {
            Log.Error($"Player {playerName} sent an invalid vend entry: {args.Entry}. Max: {entries}");
            return;
        }

        var entry = section.Entries[args.Entry];
        if (entry.Amount is <= 0)
            return;

        if (!_prototypes.TryIndex(entry.Id, out var entity))
        {
            Log.Error($"Tried to vend non-existent entity: {entry.Id}");
            return;
        }

        var playerEnt = args.Session.AttachedEntity;
        if (args.Session.AttachedEntity is not { } player)
        {
            Log.Error($"Player {playerName} tried to buy {entry.Id} without an attached entity.");
            return;
        }

        var user = CompOrNull<CMVendorUserComponent>(playerEnt);
        if (section.Choices is { } choices)
        {
            user = EnsureComp<CMVendorUserComponent>(player);
            if (!user.Choices.TryGetValue(choices.Id, out var playerChoices))
            {
                playerChoices = 0;
                user.Choices[choices.Id] = playerChoices;
            }

            if (playerChoices >= choices.Amount)
            {
                Log.Error($"Player {playerName} tried to buy too many choices.");
                return;
            }

            user.Choices[choices.Id] = ++playerChoices;
        }

        if (entry.Points != null)
        {
            if (playerEnt == null || user == null)
            {
                Log.Error($"Player {playerName} tried to buy {entry.Id} for {entry.Points} points without having points.");
                return;
            }

            if (user.Points < entry.Points)
            {
                Log.Error($"Player {playerName} with {user.Points} tried to buy {entry.Id} for {entry.Points} points without having enough points.");
                return;
            }

            user.Points -= entry.Points.Value;
            Dirty(playerEnt.Value, user);
        }

        if (entry.Amount != null)
        {
            entry.Amount--;
            Dirty(vendor);
        }

        if (_net.IsClient)
            return;

        if (entity.TryGetComponent(out CMVendorBundleComponent? bundle))
        {
            foreach (var bundled in bundle.Bundle)
            {
                Grab(player, SpawnNextToOrDrop(bundled, vendor));
            }
        }
        else
        {
            Grab(player, SpawnNextToOrDrop(entry.Id, vendor));
        }
    }

    private void Grab(EntityUid player, EntityUid item)
    {
        if (!HasComp<ItemComponent>(item))
            return;

        // TODO CM14 webbing first
        if (!TryComp(item, out ClothingComponent? clothing))
        {
            _hands.TryForcePickupAnyHand(player, item);
            return;
        }

        var equipped = false;
        foreach (var order in _order)
        {
            if ((clothing.Slots & order) == 0)
                continue;

            if (!_inventory.TryGetContainerSlotEnumerator(player, out var slots, clothing.Slots))
                return;

            while (slots.MoveNext(out var slot))
            {
                if (_inventory.TryEquip(player, item, slot.ID))
                {
                    equipped = true;
                    break;
                }
            }

            if (equipped)
                break;
        }

        if (!equipped)
            _hands.TryForcePickupAnyHand(player, item);
    }
}
