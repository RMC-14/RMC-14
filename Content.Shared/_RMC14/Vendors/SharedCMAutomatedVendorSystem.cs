using System.Numerics;
using Content.Shared._RMC14.Holiday;
using Content.Shared._RMC14.Inventory;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Scaling;
using Content.Shared._RMC14.Tools;
using Content.Shared._RMC14.Webbing;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Clothing.Components;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Content.Shared.Roles.Jobs;
using Content.Shared.UserInterface;
using Content.Shared.Wall;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Vendors;

public abstract class SharedCMAutomatedVendorSystem : EntitySystem
{
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly SharedCMInventorySystem _cmInventory = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedJobSystem _job = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedWebbingSystem _webbing = default!;
    [Dependency] private readonly SharedRMCHolidaySystem _rmcHoliday = default!;

    // TODO RMC14 make this a prototype
    public const string SpecialistPoints = "Specialist";

    private readonly Dictionary<EntProtoId, CMVendorEntry> _entries = new();
    private readonly List<CMVendorEntry> _boxEntries = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<MarineScaleChangedEvent>(OnMarineScaleChanged);

        SubscribeLocalEvent<CMAutomatedVendorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<CMAutomatedVendorComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<CMAutomatedVendorComponent, ActivatableUIOpenAttemptEvent>(OnUIOpenAttempt);
        SubscribeLocalEvent<CMAutomatedVendorComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<CMAutomatedVendorComponent, RMCAutomatedVendorHackDoAfterEvent>(OnHack);

        SubscribeLocalEvent<RMCRecentlyVendedComponent, GotEquippedHandEvent>(OnRecentlyGotEquipped);
        SubscribeLocalEvent<RMCRecentlyVendedComponent, GotEquippedEvent>(OnRecentlyGotEquipped);

        Subs.BuiEvents<CMAutomatedVendorComponent>(CMAutomatedVendorUI.Key, subs =>
        {
            subs.Event<CMVendorVendBuiMsg>(OnVendBui);
        });
    }

    private void OnMarineScaleChanged(ref MarineScaleChangedEvent ev)
    {
        var vendors = EntityQueryEnumerator<CMAutomatedVendorComponent>();
        while (vendors.MoveNext(out var uid, out var vendor))
        {
            var changed = false;
            foreach (var section in vendor.Sections)
            {
                foreach (var entry in section.Entries)
                {
                    if (entry.Multiplier is not { } multiplier ||
                        entry.Max is not { } max ||
                        entry.Box != null)
                    {
                        continue;
                    }

                    var newMax = (int) Math.Round(ev.New * multiplier);
                    var toAdd = newMax - max;
                    if (toAdd <= 0)
                        continue;

                    entry.Amount += toAdd;
                    entry.Max += toAdd;
                    changed = true;
                    AmountUpdated((uid, vendor), entry);
                }
            }

            if (changed)
                Dirty(uid, vendor);
        }
    }

    private void OnMapInit(Entity<CMAutomatedVendorComponent> ent, ref MapInitEvent args)
    {
        _entries.Clear();
        _boxEntries.Clear();
        foreach (var section in ent.Comp.Sections)
        {
            foreach (var entry in section.Entries)
            {
                _entries.TryAdd(entry.Id, entry);
                if (entry.Box != null)
                {
                    _boxEntries.Add(entry);
                    continue;
                }

                entry.Multiplier = entry.Amount;
                entry.Max = entry.Amount;
            }
        }

        foreach (var boxEntry in _boxEntries)
        {
            if (boxEntry.Box is not { } box)
                continue;

            if (_entries.TryGetValue(box, out var entry))
                AmountUpdated(ent, entry);
        }

        if (_boxEntries.Count > 0)
            Dirty(ent);
    }

    private void OnExamined(Entity<CMAutomatedVendorComponent> ent, ref ExaminedEvent args)
    {
        if (!_skills.HasSkill(args.Examiner, ent.Comp.HackSkill, ent.Comp.HackSkillLevel))
            return;

        using (args.PushGroup(nameof(CMAutomatedVendorComponent)))
        {
            args.PushMarkup(Loc.GetString("rmc-vending-machine-can-hack"));
        }
    }

    private void OnUIOpenAttempt(Entity<CMAutomatedVendorComponent> vendor, ref ActivatableUIOpenAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (HasComp<BypassInteractionChecksComponent>(args.User))
            return;

        if (vendor.Comp.Hacked)
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
                    _popup.PopupClient(Loc.GetString("cm-vending-machine-wrong-card"), vendor, args.User);
                    args.Cancel();
                    return;
                }
            }
        }

        if (vendor.Comp.Jobs.Count == 0)
            return;

        if (_mind.TryGetMind(args.User, out var mindId, out _))
        {
            foreach (var job in vendor.Comp.Jobs)
            {
                if (_job.MindHasJobWithId(mindId, job.Id))
                    return;
            }
        }

        _popup.PopupClient(Loc.GetString("cm-vending-machine-access-denied"), vendor, args.User);
        args.Cancel();
    }

    private void OnInteractUsing(Entity<CMAutomatedVendorComponent> ent, ref InteractUsingEvent args)
    {
        if (!HasComp<MultitoolComponent>(args.Used))
            return;

        args.Handled = true;
        if (!ent.Comp.Hackable)
        {
            _popup.PopupClient(Loc.GetString("rmc-vending-machine-cannot-hack", ("vendor", ent)), ent, args.User);
            return;
        }

        if (!_skills.HasSkill(args.User, ent.Comp.HackSkill, ent.Comp.HackSkillLevel))
        {
            var msg = Loc.GetString("rmc-vending-machine-hack-no-skill", ("vendor", ent));
            _popup.PopupClient(msg, ent, args.User, PopupType.SmallCaution);
            return;
        }

        var delay = ent.Comp.HackDelay * _skills.GetSkillDelayMultiplier(args.User, ent.Comp.HackSkill);
        var ev = new RMCAutomatedVendorHackDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, args.User, delay, ev, ent, ent, args.Used);
        if (_doAfter.TryStartDoAfter(doAfter))
        {
            _popup.PopupClient(Loc.GetString("rmc-vending-machine-hack-start", ("vendor", ent)), ent, args.User);
        }
    }

    private void OnHack(Entity<CMAutomatedVendorComponent> ent, ref RMCAutomatedVendorHackDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        ent.Comp.Hacked = !ent.Comp.Hacked;
        Dirty(ent);

        var msg = ent.Comp.Hacked
            ? Loc.GetString("rmc-vending-machine-hack-finish-remove", ("vendor", ent))
            : Loc.GetString("rmc-vending-machine-hack-finish-restore", ("vendor", ent));
        _popup.PopupClient(msg, ent, args.User);

        if (TryComp(ent, out AccessReaderComponent? accessReader))
        {
            var access = ent.Comp.Hacked ? new List<ProtoId<AccessLevelPrototype>>() : ent.Comp.Access;
            _accessReader.SetAccesses(ent, accessReader,access);
        }
    }

    private void OnRecentlyGotEquipped<T>(Entity<RMCRecentlyVendedComponent> ent, ref T args)
    {
        RemCompDeferred<WallMountComponent>(ent);
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
        if (section.TakeOne is { } takeOne)
        {
            user = EnsureComp<CMVendorUserComponent>(actor);
            if (!user.TakeOne.Add(takeOne))
            {
                Log.Error($"{ToPrettyString(actor)} tried to buy too many take-ones.");
                return;
            }

            Dirty(actor, user);
        }

        var validJob = true;
        if (_mind.TryGetMind(args.Actor, out var mindId, out _))
        {
            foreach (var job in section.Jobs)
            {
                if (!_job.MindHasJobWithId(mindId, job.Id))
                    validJob = false;
                else
                    validJob = true;
            }
        }

        if (!validJob)
            return;

        var validHoliday = section.Holidays.Count == 0;
        foreach (var holiday in section.Holidays)
        {
            if (_rmcHoliday.IsActiveHoliday(holiday))
                validHoliday = true;
        }

        if (!validHoliday)
            return;

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

        void ResetChoices()
        {
            if (section.Choices is { } choices && user != null)
                user.Choices[choices.Id]--;
            if (section.TakeOne is { } takeOne && user != null)
                user.TakeOne.Remove(takeOne);
        }

        if (section.SharedSpecLimit is { } globalLimit)
        {
            if (HasComp<RMCVendorSpecialistComponent>(vendor))
            {
                var thisSpecVendor = Comp<RMCVendorSpecialistComponent>(vendor);
                int vendCount = 0;

                // If the vendor's own value is at or above the capacity, immediately return.
                if (thisSpecVendor.GlobalSharedVends.TryGetValue(args.Entry, out vendCount) && vendCount >= section.SharedSpecLimit)
                {
                    // FIXME
                    ResetChoices();
                    _popup.PopupEntity(Loc.GetString("cm-vending-machine-specialist-max"), vendor, actor);
                    return;
                }

                // Get every RMCVendorSpec
                var specVendors = EntityQueryEnumerator<RMCVendorSpecialistComponent>();
                // Used to verify newer vendors
                int maxAmongVendors = 0;

                if (thisSpecVendor.GlobalSharedVends.TryGetValue(args.Entry, out vendCount))
                    // So it doesn't matter what order the vendors are checked in
                    maxAmongVendors = vendCount;

                // Goes through each RMCVendorSpec and gets the largest value for this kit type.
                while (specVendors.MoveNext(out var vendorId, out _))
                {
                    var specVendorComponent = EnsureComp<RMCVendorSpecialistComponent>(vendorId);
                    if (specVendorComponent.GlobalSharedVends.TryGetValue(args.Entry, out vendCount))
                    {
                        if (vendCount > maxAmongVendors)
                        {
                            maxAmongVendors = specVendorComponent.GlobalSharedVends[args.Entry];
                        }
                        else
                        {
                            specVendorComponent.GlobalSharedVends[args.Entry] = maxAmongVendors;
                        }
                    }
                    else // Does not exist on the currently checked vendor
                        specVendorComponent.GlobalSharedVends.Add(args.Entry, maxAmongVendors);
                    Dirty(vendorId, specVendorComponent);
                }

                thisSpecVendor.GlobalSharedVends[args.Entry] = maxAmongVendors;

                if (thisSpecVendor.GlobalSharedVends[args.Entry] >= section.SharedSpecLimit)
                {
                    ResetChoices();
                    _popup.PopupEntity(Loc.GetString("cm-vending-machine-specialist-max"), vendor.Owner, actor);
                    return;
                }
                else
                {
                    thisSpecVendor.GlobalSharedVends[args.Entry] += 1;
                }

                Dirty(vendor, thisSpecVendor);
            }
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
            if (entry.Box is { } box)
            {
                var foundEntry = false;
                foreach (var vendorSection in vendor.Comp.Sections)
                {
                    foreach (var vendorEntry in vendorSection.Entries)
                    {
                        if (vendorEntry.Id != box)
                            continue;

                        vendorEntry.Amount -= GetBoxRemoveAmount(entry);
                        entry.Amount--;
                        foundEntry = true;
                        break;
                    }

                    if (foundEntry)
                        break;
                }

                if (foundEntry)
                    Dirty(vendor);
            }
            else
            {
                entry.Amount--;
                Dirty(vendor);
                AmountUpdated(vendor, entry);
            }
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
        var recently = EnsureComp<RMCRecentlyVendedComponent>(spawn);
        var anchored = _rmcMap.GetAnchoredEntitiesEnumerator(spawn);
        while (anchored.MoveNext(out var uid))
        {
            recently.PreventCollide.Add(uid);
        }

        Dirty(spawn, recently);

        var mount = EnsureComp<WallMountComponent>(spawn);
        mount.Arc = Angle.FromDegrees(360);
        Dirty(spawn, mount);

        var grabbed = Grab(player, spawn);
        if (!grabbed && TryComp(spawn, out TransformComponent? xform))
            _transform.SetLocalPosition(spawn, xform.LocalPosition + offset, xform);

        var ev = new RMCAutomatedVendedUserEvent(spawn);
        RaiseLocalEvent(player, ref ev);
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
                    _webbing.Attach((contained, clothing), item, player, out _))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public void SetPoints(Entity<CMVendorUserComponent> user, int points)
    {
        user.Comp.Points = points;
        Dirty(user);
    }

    public void SetExtraPoints(Entity<CMVendorUserComponent> user, string key, int points)
    {
        user.Comp.ExtraPoints ??= new Dictionary<string, int>();
        user.Comp.ExtraPoints[key] = points;
        Dirty(user);
    }

    public void AmountUpdated(Entity<CMAutomatedVendorComponent> vendor, CMVendorEntry entry)
    {
        foreach (var section in vendor.Comp.Sections)
        {
            if (!section.HasBoxes)
                continue;

            foreach (var sectionEntry in section.Entries)
            {
                if (sectionEntry.Box is not { } box)
                    continue;

                if (entry.Id != box)
                    continue;

                sectionEntry.Amount = entry.Amount / GetBoxRemoveAmount(sectionEntry);
            }
        }
    }

    private int GetBoxRemoveAmount(CMVendorEntry entry)
    {
        if (entry.BoxSlots is not { } boxSlots)
        {
            if (!_prototypes.TryIndex(entry.Id, out var boxProto) ||
                !boxProto.TryGetComponent(out CMItemSlotsComponent? slots, _compFactory) ||
                slots.Count is not { } count)
            {
                return 1;
            }

            boxSlots = count;
        }

        var amount = boxSlots;
        if (entry.BoxAmount is { } boxAmount)
            amount = boxAmount;

        return Math.Max(1, amount);
    }
}
