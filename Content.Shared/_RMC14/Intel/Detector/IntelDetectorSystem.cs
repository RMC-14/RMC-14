using Content.Shared._RMC14.Inventory;
using Content.Shared._RMC14.MotionDetector;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.Coordinates;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
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

    private EntityQuery<IntelDetectorComponent> _detectorQuery;
    private EntityQuery<StorageComponent> _storageQuery;

    private readonly HashSet<Entity<IntelDetectorTrackedComponent>> _tracked = new();

    public override void Initialize()
    {
        _detectorQuery = GetEntityQuery<IntelDetectorComponent>();
        _storageQuery = GetEntityQuery<StorageComponent>();

        SubscribeLocalEvent<XenoParasiteInfectEvent>(OnXenoInfect);
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);

        SubscribeLocalEvent<IntelDetectorComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<IntelDetectorComponent, ActivateInWorldEvent>(OnActivateInWorld);
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

    private void OnExamined(Entity<IntelDetectorComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(IntelDetectorComponent)))
        {
            var mode = ent.Comp.Short ? "short" : "long";
            args.PushMarkup($"The motion detector is in [color=cyan]{mode}[/color] scanning mode.");
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

            detector.Blips.Clear();
            foreach (var tracked in _tracked)
            {
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
