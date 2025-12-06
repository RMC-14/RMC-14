using System.Linq;
using System.Numerics;
using Content.Shared._RMC14.Animations;
using Content.Shared._RMC14.Armor;
using Content.Shared._RMC14.Attachable.Components;
using Content.Shared._RMC14.Cassette;
using Content.Shared._RMC14.Holiday;
using Content.Shared._RMC14.Inventory;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.Roles.Ranks;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Medical.IV;
using Content.Shared._RMC14.Medical.Refill;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.Scaling;
using Content.Shared._RMC14.TacticalMap;
using Content.Shared._RMC14.Tools;
using Content.Shared._RMC14.Weapons.Ranged.Ammo.BulletBox;
using Content.Shared._RMC14.Weapons.Ranged.Chamber;
using Content.Shared._RMC14.Weapons.Ranged.Flamer;
using Content.Shared._RMC14.Webbing;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Clothing.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Coordinates;
using Content.Shared.Database;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Light.Components;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Content.Shared.PowerCell.Components;
using Content.Shared.Roles.Jobs;
using Content.Shared.Stacks;
using Content.Shared.Storage;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Content.Shared.UserInterface;
using Content.Shared.Verbs;
using Content.Shared.Wall;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Vendors;

public abstract class SharedCMAutomatedVendorSystem : EntitySystem
{
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly RMCPlanetSystem _rmcPlanet = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedCMInventorySystem _cmInventory = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedJobSystem _job = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedRankSystem _rank = default!;
    [Dependency] private readonly SharedRMCAnimationSystem _rmcAnimation = default!;
    [Dependency] private readonly SharedRMCHolidaySystem _rmcHoliday = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedWebbingSystem _webbing = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly SquadSystem _squads = default!;
    [Dependency] private readonly TagSystem _tags = default!;
    [Dependency] private readonly ThrowingSystem _throwingSystem = default!;

    // TODO RMC14 make this a prototype
    public const string SpecialistPoints = "Specialist";

    private readonly Dictionary<EntProtoId, CMVendorEntry> _entries = new();
    private readonly List<CMVendorEntry> _boxEntries = new();
    private static readonly ProtoId<ReagentPrototype> FlamerTankReagent = "RMCNapalmUT";

    public override void Initialize()
    {
        SubscribeLocalEvent<MarineScaleChangedEvent>(OnMarineScaleChanged);

        SubscribeLocalEvent<CMAutomatedVendorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<CMAutomatedVendorComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<CMAutomatedVendorComponent, ActivatableUIOpenAttemptEvent>(OnUIOpenAttempt);
        SubscribeLocalEvent<CMAutomatedVendorComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<CMAutomatedVendorComponent, GetVerbsEvent<AlternativeVerb>>(OnAltInteractRestockVerb);
        SubscribeLocalEvent<CMAutomatedVendorComponent, RMCAutomatedVendorHackDoAfterEvent>(OnHack);
        SubscribeLocalEvent<CMAutomatedVendorComponent, DestructionEventArgs>(OnVendorDestruction);
        SubscribeLocalEvent<CMAutomatedVendorComponent, RMCVendorRestockFromStorageDoAfterEvent>(OnRestockFromContainer);

        SubscribeLocalEvent<RMCRecentlyVendedComponent, GotEquippedHandEvent>(OnRecentlyGotEquipped);
        SubscribeLocalEvent<RMCRecentlyVendedComponent, GotEquippedEvent>(OnRecentlyGotEquipped);

        Subs.BuiEvents<CMAutomatedVendorComponent>(CMAutomatedVendorUI.Key,
            subs =>
            {
                subs.Event<CMVendorVendBuiMsg>(OnVendBui);
            });
    }

    private void OnMarineScaleChanged(ref MarineScaleChangedEvent ev)
    {
        var vendors = EntityQueryEnumerator<CMAutomatedVendorComponent>();
        while (vendors.MoveNext(out var uid, out var vendor))
        {
            if (!vendor.Scaling)
                continue;

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

                    var newMax = (int)Math.Round(ev.New * multiplier);
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
        var transform = Transform(ent.Owner);
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

                // Scale the vendors if it's a colony vendor
                if (_rmcPlanet.IsOnPlanet(transform))
                {
                    if (entry.Amount is not { } originalAmount)
                        continue;

                    if (ent.Comp.RandomUnstockAmount is { } randomUnstock)
                    {
                        if (randomUnstock == -1)
                            entry.Amount = _random.Next(1, originalAmount);
                        else
                            entry.Amount = _random.Next(1, randomUnstock);
                    }

                    if (ent.Comp.RandomEmptyChance is { } emptyChance && _random.Prob(emptyChance))
                        entry.Amount = 0;
                }
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

        if (TryComp<CMVendorUserComponent>(args.User, out var vendorUser) &&
            TryComp<RMCVendorUserRechargeComponent>(args.User, out var recharge))
        {
            var ticks = (_timing.CurTime - recharge.LastUpdate) / recharge.TimePerUpdate;
            var points = (int)Math.Floor(ticks * recharge.PointsPerUpdate);
            if (points > 0)
            {
                vendorUser.Points = Math.Min(recharge.MaxPoints, vendorUser.Points + points);
                recharge.LastUpdate = _timing.CurTime;
                DirtyEntity(args.User);
            }
        }

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

        _mind.TryGetMind(args.User, out var mindId, out _);
        foreach (var job in vendor.Comp.Jobs)
        {
            if (mindId.Valid && _job.MindHasJobWithId(mindId, job.Id))
                return;

            if (vendorUser?.Id == job)
                return;
        }

        if (vendor.Comp.Ranks.Count == 0)
            return;

        foreach (var rank in vendor.Comp.Ranks)
        {
            var userRank = _rank.GetRank(args.User);

            if (userRank != null && userRank == rank)
                return;
        }

        _popup.PopupClient(Loc.GetString("cm-vending-machine-access-denied"), vendor, args.User);
        args.Cancel();
    }

    private void OnAltInteractRestockVerb(Entity<CMAutomatedVendorComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!ent.Comp.CanManualRestock)
            return;
        if (!args.CanAccess || !args.CanInteract)
            return;
        if (!_hands.TryGetActiveItem(args.User, out var heldItem))
            return;

        var user = args.User;
        var item = heldItem.Value;

        var ignoreBulkRestock = false;
        var itemProtoId = MetaData(item).EntityPrototype?.ID;
        if (itemProtoId != null)
            ignoreBulkRestock = ent.Comp.IgnoreBulkRestockById.Contains(itemProtoId) || IgnoreBulkRestockByComponent(item);

        if (TryComp<StorageComponent>(item, out var storage) && !ignoreBulkRestock)
        {
            // Bulk restock verb for storage containers
            args.Verbs.Add(new AlternativeVerb
            {
                Text = Loc.GetString("rmc-vending-machine-restock-bulk-verb"),
                Act = () => TryRestockFromContainer(ent, item, user, storage),
                Priority = -1
            });
        }
        else
        {
            // Single item restock verb
            args.Verbs.Add(new AlternativeVerb
            {
                Text = Loc.GetString("rmc-vending-machine-restock-single-verb"),
                Act = () => TryRestockSingleItem(ent, item, user),
                Priority = -1
            });
        }
    }

    private void OnInteractUsing(Entity<CMAutomatedVendorComponent> ent, ref InteractUsingEvent args)
    {
        if (!HasComp<MultitoolComponent>(args.Used))
            return;

        args.Handled = true;
        TryHackVendor(ent, args.User, args.Used);
    }

    private void TryHackVendor(Entity<CMAutomatedVendorComponent> ent, EntityUid user, EntityUid multitool)
    {
        if (!ent.Comp.Hackable)
        {
            _popup.PopupClient(Loc.GetString("rmc-vending-machine-cannot-hack", ("vendor", ent)), ent, user);
            return;
        }

        if (!_skills.HasSkill(user, ent.Comp.HackSkill, ent.Comp.HackSkillLevel))
        {
            var msg = Loc.GetString("rmc-vending-machine-hack-no-skill", ("vendor", ent));
            _popup.PopupClient(msg, ent, user, PopupType.SmallCaution);
            return;
        }

        var delay = ent.Comp.HackDelay * _skills.GetSkillDelayMultiplier(user, ent.Comp.HackSkill);
        var ev = new RMCAutomatedVendorHackDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, user, delay, ev, ent, ent, multitool);
        if (_doAfter.TryStartDoAfter(doAfter))
        {
            _popup.PopupClient(Loc.GetString("rmc-vending-machine-hack-start", ("vendor", ent)), ent, user);
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
            _accessReader.SetAccesses((ent, accessReader), access);
        }
    }

    private void OnVendorDestruction(Entity<CMAutomatedVendorComponent> vendor, ref DestructionEventArgs args)
    {
        if (vendor.Comp.EjectContentsOnDestruction)
            EjectAllVendorContents(vendor);
    }

    private void EjectAllVendorContents(Entity<CMAutomatedVendorComponent> vendor)
    {
        // Get all available items with their quantity
        var inventory = GetAvailableInventoryWithAmounts(vendor.Comp);

        foreach (var (itemId, amount) in inventory)
        {
            // Create items in quantity amount
            for (int i = 0; i < amount; i++)
            {
                // Create item near the vendor
                var coords = Transform(vendor).Coordinates;
                var spawnedItem = Spawn(itemId, coords);

                // Throw in a random direction with a random force
                var direction = new Vector2(_random.NextFloat(-1, 1), _random.NextFloat(-1, 1));
                var throwForce = _random.NextFloat(1f, 7f);
                _throwingSystem.TryThrow(spawnedItem, direction, throwForce);
            }
        }
    }

    private List<(EntProtoId Id, int Amount)> GetAvailableInventoryWithAmounts(CMAutomatedVendorComponent component)
    {
        var inventory = new List<(EntProtoId Id, int Amount)>();

        foreach (var section in component.Sections)
        {
            foreach (var entry in section.Entries)
            {
                if (entry.Amount > 0)
                {
                    inventory.Add((entry.Id, entry.Amount.Value));
                }
            }
        }

        return inventory;
    }

    private void OnRecentlyGotEquipped<T>(Entity<RMCRecentlyVendedComponent> ent, ref T args)
    {
        RemCompDeferred<WallMountComponent>(ent);
    }

    protected virtual void OnVendBui(Entity<CMAutomatedVendorComponent> vendor, ref CMVendorVendBuiMsg args)
    {
        _audio.PlayPredicted(vendor.Comp.Sound, vendor, args.Actor);
        _rmcAnimation.TryFlick(vendor.Owner, vendor.Comp.AnimationSprite, vendor.Comp.BaseSprite);

        if (_net.IsClient)
            return;

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
                {
                    validJob = true;
                    break;
                }
            }
        }

        var validRank = true;
        foreach (var rank in section.Ranks)
        {
            var userRank = _rank.GetRank(actor);
            if (userRank == null || rank != userRank)
                validRank = false;
            else
            {
                validRank = true;
                break;
            }
        }

        if (!validJob || !validRank)
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

                // If the vendor's own value is at or above the capacity, immediately return.
                if (thisSpecVendor.GlobalSharedVends.TryGetValue(args.Entry, out var vendCount) &&
                    vendCount >= section.SharedSpecLimit)
                {
                    // FIXME
                    ResetChoices();
                    _popup.PopupEntity(Loc.GetString("cm-vending-machine-specialist-max"), vendor, actor);
                    return;
                }

                // Get every RMCVendorSpec
                var specVendors = EntityQueryEnumerator<RMCVendorSpecialistComponent>();
                // Used to verify newer vendors
                var maxAmongVendors = 0;

                if (thisSpecVendor.GlobalSharedVends.TryGetValue(args.Entry, out vendCount))
                    // So it doesn't matter what order the vendors are checked in
                    maxAmongVendors = vendCount;

                // Goes through each RMCVendorSpec and gets the largest value for this kit type.
                while (specVendors.MoveNext(out var vendorId, out _))
                {
                    var specVendorComponent = EnsureComp<RMCVendorSpecialistComponent>(vendorId);
                    foreach (var linkedEntry in args.LinkedEntries)
                    {
                        specVendorComponent.GlobalSharedVends.TryGetValue(linkedEntry, out var linkedCount);
                        maxAmongVendors += linkedCount;
                    }

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

                thisSpecVendor.GlobalSharedVends[args.Entry] += 1;
                Dirty(vendor, thisSpecVendor);
            }
        }

        if (entry.Points != null)
        {
            if (user == null)
            {
                Log.Error(
                    $"{ToPrettyString(actor)} tried to buy {entry.Id} for {entry.Points} points without having points.");
                return;
            }

            var userPoints = vendor.Comp.PointsType == null
                ? user.Points
                : user.ExtraPoints?.GetValueOrDefault(vendor.Comp.PointsType) ?? 0;
            if (userPoints < entry.Points)
            {
                Log.Error(
                    $"{ToPrettyString(actor)} with {user.Points} tried to buy {entry.Id} for {entry.Points} points without having enough points.");
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
                        Dirty(vendor);
                        AmountUpdated(vendor, vendorEntry);
                        foundEntry = true;
                        break;
                    }

                    if (foundEntry)
                        break;
                }
            }
            else
            {
                entry.Amount--;
                Dirty(vendor);
                AmountUpdated(vendor, entry);
            }
        }
        // Check if we just vended the last item, and it has partial stacks. This needs to happen AFTER Amount-- to detect when we hit 0.
        EntProtoId? partialStackItemId = null;
        int? partialStackAmount = null;
        if (entry.Amount == 0 &&
            _prototypes.TryIndex(entry.Id, out var entryProto) &&
            entryProto.TryGetComponent(out StackComponent? entryStack, _compFactory) &&
            comp.PartialProductStacks.TryGetValue(entryStack.StackTypeId, out var partial) &&
            partial > 0)
        {
            partialStackItemId = entry.Id;
            partialStackAmount = partial;
            comp.PartialProductStacks[entryStack.StackTypeId] = 0; // Clear the partial after using it
            Dirty(vendor);
        }

        if (entry.GiveSquadRoleName != null || entry.GiveIcon != null)
        {
            var overrideComp = EnsureComp<RMCVendorRoleOverrideComponent>(actor);
            overrideComp.GiveSquadRoleName = entry.GiveSquadRoleName;
            overrideComp.IsAppendSquadRoleName = entry.IsAppendSquadRoleName;
            overrideComp.GiveIcon = entry.GiveIcon;
            Dirty(actor, overrideComp);

            _squads.UpdateSquadTitle(actor);
        }

        if (entry.GiveMapBlip != null)
        {
            var mapBlip = EnsureComp<MapBlipIconOverrideComponent>(actor);
            mapBlip.Icon = entry.GiveMapBlip;
            Dirty(actor, mapBlip);
        }

        if (entry.GivePrefix != null)
        {
            var jobPrefix = EnsureComp<JobPrefixComponent>(actor);
            if (entry.IsAppendPrefix)
                jobPrefix.AdditionalPrefix = entry.GivePrefix;
            else
                jobPrefix.Prefix = entry.GivePrefix.Value;

            Dirty(actor, jobPrefix);
        }

        var min = comp.MinOffset;
        var max = comp.MaxOffset;
        for (var i = 0; i < entry.Spawn; i++)
        {
            var offset = _random.NextVector2Box(min.X, min.Y, max.X, max.Y);
            var currentPartialAmount = i == 0 ? partialStackAmount : null;
            var currentPartialItemId = i == 0 ? partialStackItemId : null;
            if (entity.TryGetComponent(out CMVendorBundleComponent? bundle, _compFactory))
            {
                foreach (var bundled in bundle.Bundle)
                {
                    // Only apply partial stack to the specific item that has it
                    var bundledPartialAmount = bundled == currentPartialItemId ? currentPartialAmount : null;
                    Vend(vendor, actor, bundled, offset, bundledPartialAmount);
                }
            }
            else
            {
                Vend(vendor, actor, entry.Id, offset, currentPartialAmount);
            }
        }

        if (entity.TryGetComponent(out CMChangeUserOnVendComponent? change, _compFactory) &&
            change.AddComponents != null)
        {
            EntityManager.AddComponents(actor, change.AddComponents);
        }
    }

    private void Vend(EntityUid vendor, EntityUid player, EntProtoId toVend, Vector2 offset, int? partialStackAmount = null)
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

        if (TryComp(vendor, out RMCRequisitionsVendorComponent? vendorComponent) && vendorComponent.Enabled &&
            _rmcMap.HasAnchoredEntityEnumerator<RMCRequisitionsChairComponent>(player.ToCoordinates(),
                out var requisitionsChair))
        {
            var itemPlacementOffset = requisitionsChair.Comp.OffsetItem;
            var finalPlacementCoordinates = requisitionsChair.Owner.ToCoordinates().Offset(itemPlacementOffset);
            var spawn = SpawnAtPosition(toVend, finalPlacementCoordinates);
            // Apply partial stack amount if specified
            if (partialStackAmount.HasValue && TryComp<StackComponent>(spawn, out var stack))
            {
                _stack.SetCount(spawn, partialStackAmount.Value, stack);
            }

            AfterVend(spawn, player, vendor, offset, true);
        }
        else
        {
            var spawn = SpawnNextToOrDrop(toVend, vendor);
            // Apply partial stack amount if specified
            if (partialStackAmount.HasValue && TryComp<StackComponent>(spawn, out var stack))
            {
                _stack.SetCount(spawn, partialStackAmount.Value, stack);
            }

            AfterVend(spawn, player, vendor, offset);
        }
    }

    private void AfterVend(EntityUid spawn, EntityUid player, EntityUid vendor, Vector2 offset, bool vended = false)
    {
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

        if (!vended)
        {
            var grabbed = Grab(player, spawn);
            if (!grabbed && TryComp(spawn, out TransformComponent? xform))
                _transform.SetLocalPosition(spawn, xform.LocalPosition + offset, xform);
        }

        var ev = new RMCAutomatedVendedUserEvent(spawn);
        RaiseLocalEvent(player, ref ev);

        _adminLog.Add(LogType.RMCVend,
            $"{ToPrettyString(player)} vended {ToPrettyString(spawn)} from vendor {ToPrettyString(vendor)}");
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

        if (_cmInventory.TryEquipClothing(player, (item, clothing), doRangeCheck: false))
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

    private void TryRestockFromContainer(Entity<CMAutomatedVendorComponent> vendor, EntityUid container, EntityUid user, StorageComponent storage)
    {
        if (_net.IsClient)
            return;

        if (storage.Container.ContainedEntities.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("rmc-vending-machine-restock-empty", ("container", container)), vendor, user);
            return;
        }

        _popup.PopupEntity(Loc.GetString("rmc-vending-machine-restock-start", ("vendor", vendor), ("container", container)), vendor, user);

        StartNextRestock(vendor, container, user, storage);
    }

    private void StartNextRestock(Entity<CMAutomatedVendorComponent> vendor, EntityUid container, EntityUid user, StorageComponent storage, HashSet<NetEntity>? failedItems = null)
    {
        if (_net.IsClient)
            return;

        failedItems ??= [];

        var items = storage.Container.ContainedEntities;
        while (items.Count > 0)
        {
            var item = items[0];
            var netItem = GetNetEntity(item);

            if (failedItems.Contains(netItem))
            {
                _container.Remove(item, storage.Container);
                _container.Insert(item, storage.Container);

                if (items.All(i => failedItems.Contains(GetNetEntity(i))))
                    break;

                continue;
            }

            if (!EntityManager.EntityExists(item))
            {
                _container.Remove(item, storage.Container);
                continue;
            }
            // Skip nested storage containers to prevent recursive processing
            if (TryComp<StorageComponent>(item, out _))
            {
                _container.Remove(item, storage.Container);
                _container.Insert(item, storage.Container);
                failedItems.Add(netItem);
                continue;
            }

            var ev = new RMCVendorRestockFromStorageDoAfterEvent
            {
                Container = GetNetEntity(container),
                FailedBulkRestockItems = failedItems,
                Item = netItem,
            };
            var doAfter = new DoAfterArgs(EntityManager, user, TimeSpan.FromSeconds(1), ev, vendor, vendor, vendor)
            {
                BreakOnMove = true,
                BreakOnDamage = true,
            };

            _doAfter.TryStartDoAfter(doAfter);
            return;
        }

        _popup.PopupEntity(Loc.GetString("rmc-vending-machine-restock-finish", ("vendor", vendor), ("container", container)), vendor, user);
    }

    private void OnRestockFromContainer(Entity<CMAutomatedVendorComponent> vendor, ref RMCVendorRestockFromStorageDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        var container = GetEntity(args.Container);
        var item = GetEntity(args.Item);
        if (!TryComp(container, out StorageComponent? storage))
            return;

        if (!EntityManager.EntityExists(item))
        {
            StartNextRestock(vendor, container, args.User, storage, args.FailedBulkRestockItems);
            return;
        }

        _container.Remove(item, storage.Container);
        // Refillable items (bottles, autoinjectors) use the CMRefillableSolutionSystem
        if (HasComp<CMRefillableSolutionComponent>(item))
        {
            var vendorCoords = Transform(vendor).Coordinates;
            _interaction.InteractUsing(args.User, item, vendor, vendorCoords, checkCanInteract: false, checkCanUse: false);

            if (EntityManager.EntityExists(item))
            {
                _container.Insert(item, storage.Container);
                args.FailedBulkRestockItems.Add(GetNetEntity(item));
            }
        }
        else
        {
            // Standard vendor items: validate and restock directly
            var restocked = TryRestockSingleItem(vendor, item, args.User, valid: true);
            if (!restocked)
            {
                _container.Insert(item, storage.Container);
                args.FailedBulkRestockItems.Add(GetNetEntity(item));
            }
        }

        StartNextRestock(vendor, container, args.User, storage, args.FailedBulkRestockItems);
    }

    private bool TryRestockSingleItem(Entity<CMAutomatedVendorComponent> vendor, EntityUid item, EntityUid user, bool valid = false)
    {
        if (_net.IsClient)
            return false;

        var itemProto = MetaData(item).EntityPrototype?.ID;
        if (itemProto == null)
            return false;
        // Get the item's stack type if it's a stack (used for matching split/merged stacks)
        string? itemStackType = null;
        if (TryComp<StackComponent>(item, out var itemStackComp))
            itemStackType = itemStackComp.StackTypeId;

        CMVendorEntry? matchingEntry = null;
        foreach (var section in vendor.Comp.Sections)
        {
            foreach (var entry in section.Entries)
            {
                // Direct prototype match
                if (entry.Id == itemProto)
                {
                    matchingEntry = entry;
                    break;
                }
                // Stack type match: if both items are stacks with the same stack type, they match
                // This handles split/merged stacks (e.g., CMTraumaKit1 matching CMTraumaKit10)
                if (itemStackType != null &&
                    _prototypes.TryIndex(entry.Id, out var entryProto) &&
                    entryProto.TryGetComponent(out StackComponent? entryStackComp, _compFactory) &&
                    entryStackComp.StackTypeId == itemStackType)
                {
                    matchingEntry = entry;
                    break;
                }
            }
            if (matchingEntry != null)
                break;
        }

        var ignoreBulkRestock = vendor.Comp.IgnoreBulkRestockById.Contains(itemProto) ||
                                IgnoreBulkRestockByComponent(item);
        if (matchingEntry == null || TryComp<StorageComponent>(item, out _) && !TryComp<ClothingComponent>(item, out _) && !ignoreBulkRestock)
        {
            RestockValidationPopup(valid, "rmc-vending-machine-restock-item-invalid", vendor, user, ("item", item));
            return false;
        }

        if (matchingEntry.Amount >= matchingEntry.Max)
        {
            if (!CanTopOffPartialStack(vendor, item, matchingEntry))
            {
                RestockValidationPopup(valid, "rmc-vending-machine-restock-item-full", vendor, user, ("vendor", vendor), ("item", item));
                return false;
            }
        }

        if (!ValidateItemForRestock(vendor, item, user, valid))
            return false;
        // Try partial stack restocking first (sandbags, materials, gauze, etc.)
        if (TryRestockPartialStack(vendor, item, user, valid, matchingEntry))
            return true;

        if (!valid)
        {
            _popup.PopupEntity(Loc.GetString("rmc-vending-machine-restock-item-finish", ("vendor", vendor), ("item", item)), vendor, user);
        }

        matchingEntry.Amount++;
        Dirty(vendor);
        AmountUpdated(vendor, matchingEntry);
        QueueDel(item);
        return true;
    }

    private bool CanTopOffPartialStack(Entity<CMAutomatedVendorComponent> vendor, EntityUid item, CMVendorEntry entry)
    {
        if (!TryComp<StackComponent>(item, out var itemStack))
            return false;

        var stackTypeId = itemStack.StackTypeId;
        if (!vendor.Comp.PartialProductStacks.TryGetValue(stackTypeId, out var partialAmount) ||
            partialAmount == 0)
            return false;

        var maxStackSize = _stack.GetMaxCount(itemStack);
        var totalItems = partialAmount + itemStack.Count;
        var fullStacksCreated = totalItems / maxStackSize;
        if (fullStacksCreated <= 0)
            return true;

        if (entry.Max is not { } max || entry.Amount is not { } currentAmount)
            return true;
        // When there's an existing partial, completing it to a full stack doesn't require a new entry.
        // It's already counted in entry.Amount. Only additional full stacks beyond the first need new entries.
        var additionalStacksNeeded = fullStacksCreated - 1;
        return currentAmount + additionalStacksNeeded <= max;
    }

    /// <summary>
    /// Handles restocking of partial stacks (sandbags, materials, etc.)
    /// Calculates how many full vendor stacks can be made, tracks partial remainders, and leaves any excess in hand if exceeds vendor max.
    /// </summary>
    private bool TryRestockPartialStack(Entity<CMAutomatedVendorComponent> vendor, EntityUid item, EntityUid user, bool suppressPopup, CMVendorEntry matchingEntry)
    {
        if (!TryComp<StackComponent>(item, out var stackComp))
            return false;

        var stackTypeId = stackComp.StackTypeId;
        var maxStackSize = _stack.GetMaxCount(stackComp);
        if (maxStackSize <= 0)
            return false;

        vendor.Comp.PartialProductStacks.TryAdd(stackTypeId, 0);
        var existingPartial = vendor.Comp.PartialProductStacks[stackTypeId];
        var itemCount = stackComp.Count;

        var restockPlan = CalculateRestockPlan(existingPartial, itemCount, maxStackSize, matchingEntry);
        if (!restockPlan.CanRestock)
            return false;

        vendor.Comp.PartialProductStacks[stackTypeId] = restockPlan.FinalPartial;
        if (restockPlan.StacksToAdd > 0)
        {
            matchingEntry.Amount += restockPlan.StacksToAdd;
            AmountUpdated(vendor, matchingEntry);
        }

        Dirty(vendor);

        if (restockPlan.ItemsToConsume >= itemCount)
            QueueDel(item);
        else
            _stack.SetCount(item, itemCount - restockPlan.ItemsToConsume, stackComp);

        if (!suppressPopup)
        {
            _popup.PopupEntity(
                Loc.GetString("rmc-vending-machine-restock-item-finish", ("vendor", vendor), ("item", item)),
                vendor,
                user);
        }

        return true;
    }

    private static RestockPlan CalculateRestockPlan(int existingPartial, int itemCount, int maxStackSize, CMVendorEntry entry)
    {
        if (maxStackSize <= 0 || itemCount <= 0) // Sanity checks to prevent edge cases
        {
            return new RestockPlan { CanRestock = false };
        }

        var totalItems = existingPartial + itemCount;
        var potentialFullStacks = totalItems / maxStackSize;
        var potentialPartial = totalItems % maxStackSize;
        if (existingPartial > 0 && potentialFullStacks == 0)
        {
            // Just adding to partials without creating a full stack.
            return new RestockPlan
            {
                CanRestock = true,
                StacksToAdd = 0,
                ItemsToConsume = itemCount,
                FinalPartial = totalItems
            };
        }
        // Calculate how many NEW entries we need (entries that don't already exist)
        var stacksToAdd = existingPartial switch
        {
            0 when potentialFullStacks == 0 => 1,
            > 0 => Math.Max(0, potentialFullStacks - 1),
            _ => potentialFullStacks
        };
        // Check if we have room for all the new entries (or if no max exists)
        if (entry.Max is not { } max || entry.Amount is not { } currentAmount)
        {
            return new RestockPlan
            {
                CanRestock = true,
                StacksToAdd = stacksToAdd,
                ItemsToConsume = itemCount,
                FinalPartial = potentialPartial
            };
        }
        // Check if we can add all the new stacks
        // When existingPartial > 0, and we're at max capacity, we need limited restocking even if stacksToAdd is 0, since partial + remainder would overflow.
        var atMaxCapacity = currentAmount >= max;
        var wouldCreateFullStacks = potentialFullStacks > 0;
        if (currentAmount + stacksToAdd <= max && !(atMaxCapacity && wouldCreateFullStacks))
        {
            return new RestockPlan
            {
                CanRestock = true,
                StacksToAdd = stacksToAdd,
                ItemsToConsume = itemCount,
                FinalPartial = potentialPartial
            };
        }
        // Would exceed max - calculate limited restocking
        return CalculateLimitedRestockPlan(existingPartial, itemCount, maxStackSize, max, currentAmount);
    }

    private static RestockPlan CalculateLimitedRestockPlan(int existingPartial, int itemCount, int maxStackSize, int max, int currentAmount)
    {
        var roomForStacks = max - currentAmount;

        if (roomForStacks <= 0)
        {
            return CalculatePartialOnlyPlan(existingPartial, itemCount, maxStackSize);
        }

        var itemsNeededForFullStacks = (roomForStacks * maxStackSize) - existingPartial;
        var itemsToConsume = Math.Min(itemCount, itemsNeededForFullStacks);
        var finalPartial = (existingPartial + itemsToConsume) % maxStackSize;

        return new RestockPlan
        {
            CanRestock = true,
            StacksToAdd = roomForStacks,
            ItemsToConsume = itemsToConsume,
            FinalPartial = finalPartial
        };
    }

    private static RestockPlan CalculatePartialOnlyPlan(int existingPartial, int itemCount, int maxStackSize)
    {
        if (existingPartial == 0)
        {
            return new RestockPlan { CanRestock = false };
        }

        var canAddToPartial = maxStackSize - existingPartial;
        var itemsToConsume = Math.Min(itemCount, canAddToPartial);
        var finalPartial = existingPartial + itemsToConsume;
        if (finalPartial >= maxStackSize)
            finalPartial = 0;
        return new RestockPlan
        {
            CanRestock = true,
            StacksToAdd = 0,
            ItemsToConsume = itemsToConsume,
            FinalPartial = finalPartial
        };
    }

    /// <summary>
    /// Clears any partial stack for an entry, turning it to a full stack.
    /// Partial stacks are cleared when the vendor is at max stock.
    /// Used by the Medical Supply Link.
    /// </summary>
    /// <param name="vendor">The vendor entity.</param>
    /// <param name="entry">The vendor entry to clear partial stacks for.</param>
    /// <returns>True if a partial stack was cleared, false otherwise.</returns>
    public bool TryClearPartialStack(Entity<CMAutomatedVendorComponent> vendor, CMVendorEntry entry)
    {
        if (!_prototypes.TryIndex(entry.Id, out var entryProto) ||
            !entryProto.TryGetComponent(out StackComponent? entryStack, _compFactory))
            return false;

        var stackTypeId = entryStack.StackTypeId;
        if (!vendor.Comp.PartialProductStacks.TryGetValue(stackTypeId, out var partialAmount) ||
            partialAmount == 0)
            return false;

        vendor.Comp.PartialProductStacks[stackTypeId] = 0;
        Dirty(vendor);
        return true;
    }

    /// <summary>
    /// Validates that an item meets all requirements for restocking into the vendor.
    /// All applicable validation checks must pass for the item to be restocked.
    /// Uses short-circuit evaluation: returns false on first failed check.
    /// </summary>
    /// <param name="vendor">The vendor being restocked.</param>
    /// <param name="item">The item to validate.</param>
    /// <param name="user">The user attempting to restock.</param>
    /// <param name="valid">If true, suppress error popups (for bulk operations).</param>
    /// <returns>True if all applicable validation checks pass, false otherwise.</returns>
    private bool ValidateItemForRestock(Entity<CMAutomatedVendorComponent> vendor, EntityUid item, EntityUid user, bool valid)
    {
        return ValidateReagentContainers(item, user, valid)
               && (!HasComp<GunComponent>(item) || ValidateGun(item, user, valid))
               && ValidateAmmunition(vendor, item, user, valid)
               && ValidateEquipment(item, user, valid);
    }

    /// <summary>
    /// Validates reagent containers.
    /// Checks flamer tanks for correct fuel type and fill level and if blood packs are full.
    /// Reagent containers from We-Ya-Med Plus(bottles, hyposprays, etc.) are handled by the refilling system before restocking.
    /// </summary>
    private bool ValidateReagentContainers(EntityUid item, EntityUid user, bool valid)
    {
        if (TryComp<RMCFlamerTankComponent>(item, out var flamerTank) && !ValidateFlamerTank(item, flamerTank, user, valid))
            return false;

        if (TryComp<BloodPackComponent>(item, out var bloodPack) && !ValidateBloodPack(item, bloodPack, user, valid))
            return false;

        return true;
    }

    private bool ValidateFlamerTank(EntityUid tank, RMCFlamerTankComponent flamerTank, EntityUid user, bool valid)
    {
        if (!_solution.TryGetSolution(tank, flamerTank.SolutionId, out _, out var solution))
        {
            RestockValidationPopup(valid, "rmc-vending-machine-restock-item-invalid", tank, user, ("item", tank));
            return false;
        }

        var hasCorrectFuel = solution.Contents.Count == 1 && solution.Contents.Any(r => r.Reagent.Prototype == FlamerTankReagent);

        if (!hasCorrectFuel)
        {
            RestockValidationPopup(valid, "rmc-vending-machine-restock-flamer-tank-wrong-fuel", tank, user, ("item", tank));
            return false;
        }

        if (solution.Volume < solution.MaxVolume)
        {
            RestockValidationPopup(valid, "rmc-vending-machine-restock-flamer-tank-not-full", tank, user, ("item", tank));
            return false;
        }

        return true;
    }

    private bool ValidateBloodPack(EntityUid pack, BloodPackComponent bloodPack, EntityUid user, bool valid)
    {
        if (!_solution.TryGetSolution(pack, bloodPack.Solution, out _, out var solution))
        {
            RestockValidationPopup(valid, "rmc-vending-machine-restock-item-invalid", pack, user, ("item", pack));
            return false;
        }

        if (solution.Volume < solution.MaxVolume)
        {
            RestockValidationPopup(valid, "rmc-vending-machine-restock-blood-pack-not-full", pack, user, ("item", pack));
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validates that a gun is in proper condition for restocking.
    /// Checks for loaded magazines, chambered rounds, internal ammo, and non-standard attachments.
    /// </summary>
    private bool ValidateGun(EntityUid gun, EntityUid user, bool valid)
    {
        if (IsGunLoaded(gun))
        {
            RestockValidationPopup(valid, "rmc-vending-machine-restock-gun-loaded", gun, user, ("gun", gun));
            return false;
        }

        return ValidateGunAttachments(gun, user, valid);
    }

    private bool IsGunLoaded(EntityUid gun)
    {
        // Check magazine
        if (TryComp<MagazineAmmoProviderComponent>(gun, out _) &&
            _container.TryGetContainer(gun, "gun_magazine", out var magContainer) &&
            magContainer.ContainedEntities.Count > 0)
            return true;

        // Check chamber
        if (TryComp<RMCGunChamberComponent>(gun, out var chamber) &&
            _container.TryGetContainer(gun, chamber.ContainerId, out var chamberContainer) &&
            chamberContainer.ContainedEntities.Count > 0)
            return true;

        // Check internal ammo
        if (TryComp<BallisticAmmoProviderComponent>(gun, out var ballisticProvider) &&
            (ballisticProvider.UnspawnedCount > 0 || ballisticProvider.Entities.Count > 0))
            return true;

        return false;
    }

    private bool ValidateGunAttachments(EntityUid gun, EntityUid user, bool valid)
    {
        if (!TryComp<AttachableHolderComponent>(gun, out var holderComp))
            return true;

        var holder = new Entity<AttachableHolderComponent>(gun, holderComp);
        foreach (var slotId in holder.Comp.Slots.Keys)
        {
            var hasAttachment = _container.TryGetContainer(gun, slotId, out var slotContainer) &&
                                slotContainer.ContainedEntities.Count > 0;
            var hasStartingAttachment = holder.Comp.StartingAttachments.TryGetValue(slotId, out var startingProto);
            if (!hasAttachment && !hasStartingAttachment)
                continue;

            if (hasAttachment != hasStartingAttachment)
            {
                RestockValidationPopup(valid, "rmc-vending-machine-restock-gun-attachments", gun, user, ("gun", gun));
                return false;
            }

            var attachedEntity = slotContainer!.ContainedEntities[0];
            var attachedProto = MetaData(attachedEntity).EntityPrototype?.ID;
            if (attachedProto == null || attachedProto != startingProto)
            {
                RestockValidationPopup(valid, "rmc-vending-machine-restock-gun-attachments", gun, user, ("gun", gun));
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Validates if ammunition items (magazines, ammo boxes, etc.) are full.
    /// </summary>
    private bool ValidateAmmunition(Entity<CMAutomatedVendorComponent> vendor, EntityUid item, EntityUid user, bool valid)
    {
        if (TryComp<BallisticAmmoProviderComponent>(item, out var ammoProvider) &&
            !HasComp<GunComponent>(item) && // Skip guns with BallisticAmmoProviderComponent (flare guns, shotguns, revolvers)
            ammoProvider.Count < ammoProvider.Capacity)
        {
            RestockValidationPopup(valid, "rmc-vending-machine-restock-mag-not-full", item, user, ("mag", item));
            return false;
        }

        if (TryComp<ItemSlotsComponent>(item, out var boxSlots) &&
            !HasComp<CMHolsterComponent>(item) &&
            !ValidateMagazineBox(item, boxSlots, user, valid))
        {
            return false;
        }

        if (TryComp<BulletBoxComponent>(item, out var bulletBox) && bulletBox.Amount < bulletBox.Max)
        {
            RestockValidationPopup(valid, "rmc-vending-machine-restock-box-not-full", item, user, ("item", item));
            return false;
        }

        return ValidateStorageBasedAmmoBox(vendor, item, user, valid); // Shotgun shell boxes and similar
    }

    private bool ValidateMagazineBox(EntityUid box, ItemSlotsComponent slots, EntityUid user, bool valid)
    {
        var totalSlots = slots.Slots.Count;
        var filledSlots = 0;

        foreach (var (slotId, _) in slots.Slots)
        {
            if (!_container.TryGetContainer(box, slotId, out var container) ||
                container.ContainedEntities.Count == 0)
                continue;

            filledSlots++;
            var magazine = container.ContainedEntities[0];
            if (!TryComp<BallisticAmmoProviderComponent>(magazine, out var magAmmo))
                continue;

            if (magAmmo.Count < magAmmo.Capacity)
            {
                RestockValidationPopup(valid, "rmc-vending-machine-restock-mag-not-full", box, user, ("mag", magazine));
                return false;
            }
        }

        if (totalSlots > 1 && filledSlots < totalSlots)
        {
            RestockValidationPopup(valid, "rmc-vending-machine-restock-box-not-full", box, user, ("item", box));
            return false;
        }

        return true;
    }

    private bool ValidateStorageBasedAmmoBox(Entity<CMAutomatedVendorComponent> vendor, EntityUid item, EntityUid user, bool valid)
    {
        var itemProto = MetaData(item).EntityPrototype?.ID;
        if (itemProto == null || !vendor.Comp.IgnoreBulkRestockById.Contains(itemProto))
            return true;

        if (!TryComp<StorageComponent>(item, out var storage))
            return true;

        if (storage.Container.ContainedEntities.Count == 0)
        {
            RestockValidationPopup(valid, "rmc-vending-machine-restock-box-not-full", item, user, ("item", item));
            return false;
        }

        var maxCapacity = storage.Grid.GetArea();
        if (maxCapacity > 0 && storage.Container.ContainedEntities.Count < maxCapacity)
        {
            RestockValidationPopup(valid, "rmc-vending-machine-restock-box-not-full", item, user, ("item", item));
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validates equipment items (armor, helmets, machete holsters, flare packs, power cells) are in proper state.
    /// </summary>
    private bool ValidateEquipment(EntityUid item, EntityUid user, bool valid)
    {
        if (TryComp<StorageComponent>(item, out var storage) && storage.Container.ContainedEntities.Count > 0)
        {
            // Armor/helmets use IgnoreBulkRestockByComponent to get here.
            RestockValidationPopup(valid, "rmc-vending-machine-restock-storage-not-empty", item, user, ("item", item));
            return false;
        }

        if (HasComp<CMHolsterComponent>(item) &&
            MetaData(item).EntityName.Contains("machete", StringComparison.OrdinalIgnoreCase) &&
            !ValidateMacheteHolster(item, user, valid))
        {
            return false;
        }

        if (TryComp<CMItemSlotsComponent>(item, out var cmItemSlots) &&
            (_tags.HasTag(item, new ProtoId<TagPrototype>("CMFlarePack")) ||
             _tags.HasTag(item, new ProtoId<TagPrototype>("RMCPackFlareCAS"))) &&
            !ValidateFlarePack(item, cmItemSlots, user, valid))
        {
            return false;
        }

        if (TryComp<CassettePlayerComponent>(item, out var cassettePlayer) &&
            !ValidateCassettePlayer(item, cassettePlayer, user, valid))
        {
            return false;
        }

        return ValidatePowerCell(item, user, valid);
    }

    private bool ValidateMacheteHolster(EntityUid holster, EntityUid user, bool valid)
    {
        if (!TryComp<ItemSlotsComponent>(holster, out var itemSlots))
            return true;

        foreach (var (slotId, _) in itemSlots.Slots)
        {
            if (_container.TryGetContainer(holster, slotId, out var container) &&
                container.ContainedEntities.Count > 0)
            {
                return true;
            }
        }

        RestockValidationPopup(valid, "rmc-vending-machine-restock-machete-holster-empty", holster, user, ("item", holster));
        return false;
    }

    private bool ValidateFlarePack(EntityUid pack, CMItemSlotsComponent slots, EntityUid user, bool valid)
    {
        if (slots.Count == null)
        {
            RestockValidationPopup(valid, "rmc-vending-machine-restock-item-invalid", pack, user, ("item", pack));
            return false;
        }

        var maxSlots = slots.Count.Value;
        var slotBaseName = slots.Slot?.Name ?? "Flare";
        var filledSlotCount = 0;
        for (var slotIndex = 1; slotIndex <= maxSlots; slotIndex++)
        {
            var slotId = $"{slotBaseName}{slotIndex}";
            if (!_container.TryGetContainer(pack, slotId, out var container) ||
                container.ContainedEntities.Count == 0)
                continue;

            filledSlotCount++;

            if (!ValidateFlareCondition(container.ContainedEntities[0], pack, user, valid))
                return false;
        }

        if (filledSlotCount < maxSlots)
        {
            RestockValidationPopup(valid, "rmc-vending-machine-restock-flare-pack-not-full", pack, user, ("item", pack));
            return false;
        }

        return true;
    }

    private bool ValidateFlareCondition(EntityUid flare, EntityUid pack, EntityUid user, bool valid)
    {
        if (!TryComp<ExpendableLightComponent>(flare, out var expendable))
            return true;

        if (expendable.CurrentState != ExpendableLightState.BrandNew)
        {
            RestockValidationPopup(valid, "rmc-vending-machine-restock-flare-spent", pack, user, ("item", pack));
            return false;
        }

        return true;
    }

    private bool ValidateCassettePlayer(EntityUid player, CassettePlayerComponent cassettePlayer, EntityUid user, bool valid)
    {
        if (_container.TryGetContainer(player, cassettePlayer.ContainerId, out var container) &&
            container.ContainedEntities.Count > 0)
        {
            RestockValidationPopup(valid, "rmc-vending-machine-restock-cassette-player-has-tape", player, user, ("item", player));
            return false;
        }

        return true;
    }

    private bool ValidatePowerCell(EntityUid item, EntityUid user, bool valid)
    {
        if (!TryComp<PowerCellSlotComponent>(item, out var powerCellSlot))
            return true;

        if (!_container.TryGetContainer(item, powerCellSlot.CellSlotId, out var cellContainer) ||
            cellContainer.ContainedEntities.Count == 0)
        {
            RestockValidationPopup(valid, "rmc-vending-machine-restock-power-cell-missing", item, user, ("item", item));
            return false;
        }

        var (currentCharge, maxCharge) = GetBatteryCharge(item, powerCellSlot);
        if (maxCharge > 0 && currentCharge < maxCharge)
        {
            RestockValidationPopup(valid, "rmc-vending-machine-restock-power-cell-not-charged", item, user, ("item", item));
            return false;
        }

        return true;
    }

    protected virtual (float currentCharge, float maxCharge) GetBatteryCharge(EntityUid item, PowerCellSlotComponent powerCellSlot)
    {
        return (0, 0);
    }

    private bool IgnoreBulkRestockByComponent(EntityUid item)
    {
        // For armor/helmets because they have storage component
        return HasComp<CMArmorComponent>(item) || HasComp<CMHardArmorComponent>(item);
    }

    private void RestockValidationPopup(bool showError, string locKey, EntityUid target, EntityUid user, params (string, object)[] args)
    {
        if (showError)
            return;

        var message = Loc.GetString(locKey, args);
        _popup.PopupEntity(message, target, user);
    }
}
