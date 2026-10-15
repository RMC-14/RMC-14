using Content.Shared._RMC14.Inventory;
using Content.Shared._RMC14.MotionDetector;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.Coordinates;
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Intel.Detector;

public sealed class IntelDetectorSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedCMInventorySystem _rmcInventory = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private EntityQuery<IntelDetectorComponent> _detectorQuery;
    private EntityQuery<StorageComponent> _storageQuery;
    private EntityQuery<IntelDetectorTrackedComponent> _trackedIntelQuery;

    private readonly HashSet<Entity<IntelDetectorTrackedComponent>> _tracked = new();

    public override void Initialize()
    {
        _detectorQuery = GetEntityQuery<IntelDetectorComponent>();
        _storageQuery = GetEntityQuery<StorageComponent>();
        _trackedIntelQuery = GetEntityQuery<IntelDetectorTrackedComponent>();

        SubscribeLocalEvent<XenoParasiteInfectEvent>(OnXenoInfect);
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);

        SubscribeLocalEvent<IntelDetectorComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<IntelDetectorComponent, ActivateInWorldEvent>(OnActivateInWorld);
        SubscribeLocalEvent<IntelDetectorComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<IntelDetectorComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<IntelDetectorComponent, DroppedEvent>(OnDisable);
        SubscribeLocalEvent<IntelDetectorComponent, RMCDroppedEvent>(OnDisable);
        SubscribeLocalEvent<IntelDetectorComponent, ExaminedEvent>(OnExamined);
    }

    private void OnXenoInfect(XenoParasiteInfectEvent ev)
    {
        DisableDetectorsOnMob(ev.Target);
    }

    private void OnMobStateChanged(MobStateChangedEvent ev)
    {
        if (ev.NewMobState != MobState.Dead)
            return;

        DisableDetectorsOnMob(ev.Target);
    }

    private void OnUseInHand(Entity<IntelDetectorComponent> ent, ref UseInHandEvent args)
    {
        args.Handled = true;
        Toggle(ent);
        _audio.PlayPredicted(ent.Comp.ToggleSound, ent, args.User);
    }

    private void OnActivateInWorld(Entity<IntelDetectorComponent> ent, ref ActivateInWorldEvent args)
    {
        if (!_container.TryGetContainingContainer(ent.Owner, out var container))
            return;

        if (!_hands.IsHolding(args.User, ent.Owner) &&
            HasComp<StorageComponent>(container.Owner) &&
            !_container.TryGetContainingContainer(container.Owner, out _))
            return;

        args.Handled = true;
        Toggle(ent);
        _audio.PlayPredicted(ent.Comp.ToggleSound, ent, args.User);
    }

    private void OnAfterInteract(Entity<IntelDetectorComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target is not { } target || target != args.User)
            return;

        args.Handled = true;

        if (!ent.Comp.Enabled)
        {
            _popup.PopupClient(Loc.GetString("rmc-intel-detector-not-on"), args.User, args.User);
            return;
        }

        if (_timing.CurTime < ent.Comp.NextSelfCheckAt)
        {
            _popup.PopupClient(Loc.GetString("rmc-intel-detector-cooldown"), args.User, args.User);
            return;
        }

        var hasIntel = HasIntelOnPerson(args.User);
        var msg = hasIntel
            ? Loc.GetString("rmc-intel-detector-has-intel")
            : Loc.GetString("rmc-intel-detector-no-intel");

        _popup.PopupClient(msg, args.User, args.User);

        if (hasIntel)
        {
            _audio.PlayPredicted(ent.Comp.ScanSound, ent, args.User);
            ent.Comp.NextSelfCheckAt = _timing.CurTime + ent.Comp.SelfCheckCooldown;
            Dirty(ent);
        }
    }

    private void OnGetVerbs(Entity<IntelDetectorComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb
        {
            Text = ent.Comp.Short ? "Change to long range mode" : "Change to short range mode",
            Act = () =>
            {
                ent.Comp.Short = !ent.Comp.Short;
                Dirty(ent);
                _audio.PlayPredicted(ent.Comp.ToggleSound, ent, user);
            },
        });
    }

    private void OnDisable<T>(Entity<IntelDetectorComponent> ent, ref T args)
    {
        ent.Comp.Enabled = false;
        Dirty(ent);
        UpdateAppearance(ent);
    }

    private void DisableIntelDetectors(EntityUid ent)
    {
        if (_detectorQuery.TryComp(ent, out var detector))
        {
            detector.Enabled = false;
            Dirty(ent, detector);
            UpdateAppearance((ent, detector));
        }

        if (_storageQuery.TryComp(ent, out var storage))
        {
            foreach (var stored in storage.StoredItems.Keys)
            {
                DisableIntelDetectors(stored);
            }
        }
    }

    private void DisableDetectorsOnMob(EntityUid uid)
    {
        foreach (var held in _hands.EnumerateHeld(uid))
        {
            DisableIntelDetectors(held);
        }

        var slots = _inventory.GetSlotEnumerator(uid);
        while (slots.MoveNext(out var slot))
        {
            if (slot.ContainedEntity is { } contained)
                DisableIntelDetectors(contained);
        }
    }

    private bool HasIntelOnPerson(EntityUid uid)
    {
        foreach (var held in _hands.EnumerateHeld(uid))
        {
            if (ItemHasIntel(held))
                return true;
        }

        var slots = _inventory.GetSlotEnumerator(uid);
        while (slots.MoveNext(out var slot))
        {
            if (slot.ContainedEntity is { } contained && ItemHasIntel(contained))
                return true;
        }

        return false;
    }

    private bool ItemHasIntel(EntityUid ent)
    {
        if (_trackedIntelQuery.HasComp(ent))
            return true;

        if (_storageQuery.TryComp(ent, out var storage))
        {
            foreach (var stored in storage.StoredItems.Keys)
            {
                if (ItemHasIntel(stored))
                    return true;
            }
        }

        return false;
    }

    private bool TryGetCarrier(EntityUid item, out EntityUid carrier)
    {
        carrier = default;
        var current = item;
        while (_container.TryGetContainingContainer((current, null), out var container))
        {
            if (HasComp<HandsComponent>(container.Owner) || HasComp<InventoryComponent>(container.Owner))
            {
                carrier = container.Owner;
                return true;
            }

            current = container.Owner;
        }

        return false;
    }

    private void OnExamined(Entity<IntelDetectorComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(IntelDetectorComponent)))
        {
            var mode = ent.Comp.Short ? "short" : "long";
            args.PushMarkup($"The motion detector is in [color=cyan]{mode}[/color] scanning mode.");
            args.PushMarkup(Loc.GetString("rmc-intel-detector-self-check-examine"));
        }
    }

    public void Toggle(Entity<IntelDetectorComponent> ent)
    {
        ref var enabled = ref ent.Comp.Enabled;
        enabled = !enabled;

        if (enabled)
            ent.Comp.NextScanAt = _timing.CurTime + GetRefreshRate(ent);

        ent.Comp.Blips.Clear();
        Dirty(ent);
        UpdateAppearance(ent);
    }

    private TimeSpan GetRefreshRate(Entity<IntelDetectorComponent> ent)
    {
        return ent.Comp.Short ? ent.Comp.ShortRefresh : ent.Comp.LongRefresh;
    }

    private void UpdateAppearance(Entity<IntelDetectorComponent> ent)
    {
        _appearance.SetData(ent, IntelDetectorLayer.State, ent.Comp.Enabled);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var detectors = EntityQueryEnumerator<IntelDetectorComponent>();
        while (detectors.MoveNext(out var uid, out var detector))
        {
            if (!detector.Enabled)
                continue;

            if (time < detector.NextScanAt)
                continue;

            detector.LastScan = time;
            detector.NextScanAt = time + GetRefreshRate((uid, detector));
            Dirty(uid, detector);

            var range = detector.Short ? detector.ShortRange : detector.LongRange;
            _tracked.Clear();
            _entityLookup.GetEntitiesInRange(uid.ToCoordinates(), range, _tracked);

            TryGetCarrier(uid, out var detectorCarrier);

            detector.Blips.Clear();
            foreach (var tracked in _tracked)
            {
                if (detectorCarrier != default &&
                    TryGetCarrier(tracked.Owner, out var trackedCarrier) &&
                    trackedCarrier == detectorCarrier)
                {
                    continue;
                }

                detector.Blips.Add(new Blip(_transform.GetMapCoordinates(tracked), false));
            }

            if (detector.Blips.Count == 0)
            {
                if (_rmcInventory.TryGetUserHoldingOrStoringItem(uid, out var user))
                    _audio.PlayEntity(detector.ScanEmptySound, user, uid);

                continue;
            }

            _audio.PlayPvs(detector.ScanSound, uid);
        }
    }
}
