using System.Numerics;
using Content.Shared._CM14.Inventory;
using Content.Shared._CM14.Marines.Squads;
using Content.Shared._CM14.Webbing;
using Content.Shared.Access.Components;
using Content.Shared.Clothing.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Content.Shared.Roles.Jobs;
using Content.Shared.UserInterface;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._CM14.Vendors;

public abstract class SharedCMAutomatedVendorSystem : EntitySystem
{
    [Dependency] private readonly SharedCMInventorySystem _cmInventory = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedJobSystem _job = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedWebbingSystem _webbing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<CMAutomatedVendorComponent, ActivatableUIOpenAttemptEvent>(OnUIOpenAttempt);

        Subs.BuiEvents<CMAutomatedVendorComponent>(CMAutomatedVendorUI.Key, subs =>
        {
            subs.Event<CMVendorVendBuiMsg>(OnVendBui);
        });
    }

    private void OnUIOpenAttempt(Entity<CMAutomatedVendorComponent> vendor, ref ActivatableUIOpenAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (TryComp(vendor, out AccessReaderComponent? reader) &&
            reader.Enabled &&
            reader.AccessLists.Count > 0)
        {
            foreach (var item in _inventory.GetHandOrInventoryEntities(args.User))
            {
                if (HasComp<IdCardComponent>(item) &&
                    TryComp(item, out IdCardOwnerComponent? owner) &&
                    owner.Id != args.User)
                {
                    _popup.PopupClient("Wrong ID card owner detected.", vendor, args.User);
                    args.Cancel();
                    return;
                }
            }
        }

        if (vendor.Comp.Job is not { Id.Length: > 0 } job)
            return;

        if (!_mind.TryGetMind(args.User, out var mindId, out _) ||
            !_job.MindHasJobWithId(mindId, job.Id))
        {
            _popup.PopupClient("Access denied.", vendor, args.User);
            args.Cancel();
        }
    }

    protected virtual void OnVendBui(Entity<CMAutomatedVendorComponent> vendor, ref CMVendorVendBuiMsg args)
    {
        var comp = vendor.Comp;
        var sections = comp.Sections.Count;
        var actor = args.Actor;
        if (args.Section < 0 || args.Section >= sections)
        {
            Log.Error($"{ToPrettyString(actor)} sent an invalid vend section: {args.Section}. Max: {sections}");
            return;
        }

        var section = comp.Sections[args.Section];
        var entries = section.Entries.Count;
        if (args.Entry < 0 || args.Entry >= entries)
        {
            Log.Error($"{ToPrettyString(actor)} sent an invalid vend entry: {args.Entry}. Max: {entries}");
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

        var user = CompOrNull<CMVendorUserComponent>(actor);
        if (section.TakeAll is { } takeAll)
        {
            user = EnsureComp<CMVendorUserComponent>(actor);
            if (!user.TakeAll.Add((takeAll, entry.Id)))
            {
                Log.Error($"{ToPrettyString(actor)} tried to buy too many take-alls.");
                return;
            }

            Dirty(actor, user);
        }

        if (section.Choices is { } choices)
        {
            user = EnsureComp<CMVendorUserComponent>(actor);
            if (!user.Choices.TryGetValue(choices.Id, out var playerChoices))
            {
                playerChoices = 0;
                user.Choices[choices.Id] = playerChoices;
                Dirty(actor, user);
            }

            if (playerChoices >= choices.Amount)
            {
                Log.Error($"{ToPrettyString(actor)} tried to buy too many choices.");
                return;
            }

            user.Choices[choices.Id] = ++playerChoices;
            Dirty(actor, user);
        }

        if (entry.Points != null)
        {
            if (user == null)
            {
                Log.Error($"{ToPrettyString(actor)} tried to buy {entry.Id} for {entry.Points} points without having points.");
                return;
            }

            var userPoints = vendor.Comp.PointsType == null
                ? user.Points
                : user.ExtraPoints?.GetValueOrDefault(vendor.Comp.PointsType) ?? 0;
            if (userPoints < entry.Points)
            {
                Log.Error($"{ToPrettyString(actor)} with {user.Points} tried to buy {entry.Id} for {entry.Points} points without having enough points.");
                return;
            }

            if (vendor.Comp.PointsType == null)
                user.Points -= entry.Points.Value;
            else if (user.ExtraPoints != null)
                user.ExtraPoints[vendor.Comp.PointsType] = userPoints - (entry.Points ?? 0);

            Dirty(actor, user);
        }

        if (entry.Amount != null)
        {
            entry.Amount--;
            Dirty(vendor);
        }

        if (_net.IsClient)
            return;

        var min = comp.MinOffset;
        var max = comp.MaxOffset;
        for (var i = 0; i < entry.Spawn; i++)
        {
            var offset = _random.NextVector2Box(min.X, min.Y, max.X, max.Y);
            if (entity.TryGetComponent(out CMVendorBundleComponent? bundle, _compFactory))
            {
                foreach (var bundled in bundle.Bundle)
                {
                    Vend(vendor, actor, bundled, offset);
                }
            }
            else
            {
                Vend(vendor, actor, entry.Id, offset);
            }
        }

        if (entity.TryGetComponent(out CMChangeUserOnVendComponent? change, _compFactory) &&
            change.AddComponents != null)
        {
            EntityManager.AddComponents(actor, change.AddComponents);
        }
    }

    private void Vend(EntityUid vendor, EntityUid player, EntProtoId toVend, Vector2 offset)
    {
        if (_prototypes.Index(toVend).TryGetComponent(out CMVendorMapToSquadComponent? mapTo, _compFactory))
        {
            if (TryComp(player, out SquadMemberComponent? member) &&
                member.Squad is { } squad &&
                CompOrNull<MetaDataComponent>(squad)?.EntityPrototype is { } squadPrototype &&
                mapTo.Map.TryGetValue(squadPrototype.ID, out var mapped))
            {
                toVend = mapped;
            }
            else if (mapTo.Default is { } defaultVend)
            {
                toVend = defaultVend;
            }
            else
            {
                return;
            }
        }

        var spawn = SpawnNextToOrDrop(toVend, vendor);
        if (!Grab(player, spawn) && TryComp(spawn, out TransformComponent? xform))
            _transform.SetLocalPosition(spawn, xform.LocalPosition + offset, xform);
    }

    private bool Grab(EntityUid player, EntityUid item)
    {
        if (!HasComp<ItemComponent>(item))
            return false;

        if (TryAttachWebbing(player, item))
            return true;

        if (!TryComp(item, out ClothingComponent? clothing))
        {
            return _hands.TryPickupAnyHand(player, item);
        }

        if (_cmInventory.TryEquipClothing(player, (item, clothing)))
            return true;

        return _hands.TryPickupAnyHand(player, item);
    }

    private bool TryAttachWebbing(EntityUid player, EntityUid item)
    {
        if (HasComp<WebbingComponent>(item) &&
            _inventory.TryGetContainerSlotEnumerator(player, out var enumerator))
        {
            while (enumerator.MoveNext(out var slot))
            {
                if (slot.ContainedEntity is { } contained &&
                    TryComp(contained, out WebbingClothingComponent? clothing) &&
                    _webbing.Attach((contained, clothing), item))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
