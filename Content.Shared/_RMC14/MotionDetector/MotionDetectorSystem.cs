﻿using Content.Shared._RMC14.Inventory;
using Content.Shared._RMC14.Weapons.Ranged.Battery;
using Content.Shared.Actions;
using Content.Shared.Coordinates;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.MotionDetector;

public sealed class MotionDetectorSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly MotionDetectorSystem _motionDetector = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly RMCGunBatterySystem _rmcGunBattery = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private EntityQuery<MotionDetectorComponent> _detectorQuery;
    private EntityQuery<StorageComponent> _storageQuery;

    private readonly List<Entity<MotionDetectorTrackedComponent>> _toUpdate = new();
    private readonly HashSet<Entity<MotionDetectorTrackedComponent>> _tracked = new();

    public override void Initialize()
    {
        _detectorQuery = GetEntityQuery<MotionDetectorComponent>();
        _storageQuery = GetEntityQuery<StorageComponent>();

        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);

        SubscribeLocalEvent<MotionDetectorComponent, UseInHandEvent>(OnMotionDetectorUseInHand);
        SubscribeLocalEvent<MotionDetectorComponent, GetVerbsEvent<AlternativeVerb>>(OnMotionDetectorGetVerbs);
        SubscribeLocalEvent<MotionDetectorComponent, DroppedEvent>(OnMotionDetectorDropped);
        SubscribeLocalEvent<MotionDetectorComponent, RMCDroppedEvent>(OnMotionDetectorDropped);
        SubscribeLocalEvent<MotionDetectorComponent, ExaminedEvent>(OnMotionDetectorExamined);

        SubscribeLocalEvent<ToggleableMotionDetectorComponent, GetItemActionsEvent>(OnGetItemActions);
        SubscribeLocalEvent<ToggleableMotionDetectorComponent, ToggleableMotionDetectorActionEvent>(OnToggleAction);
        SubscribeLocalEvent<ToggleableMotionDetectorComponent, GunGetBatteryDrainEvent>(OnGetBatteryDrain);
        SubscribeLocalEvent<ToggleableMotionDetectorComponent, GunUnpoweredEvent>(OnGunUnpowered);
        SubscribeLocalEvent<ToggleableMotionDetectorComponent, MotionDetectorUpdatedEvent>(OnMotionDetectorUpdated);

        SubscribeLocalEvent<MotionDetectorTrackedComponent, MoveEvent>(OnMotionDetectorTracked);
    }

    private void OnMobStateChanged(MobStateChangedEvent ev)
    {
        if (ev.NewMobState != MobState.Dead)
            return;

        foreach (var held in _hands.EnumerateHeld(ev.Target))
        {
            DisableMotionDetectors(held);
        }

        var slots = _inventory.GetSlotEnumerator(ev.Target);
        while (slots.MoveNext(out var slot))
        {
            if (slot.ContainedEntity is { } contained)
                DisableMotionDetectors(contained);
        }
    }

    private void OnMotionDetectorUseInHand(Entity<MotionDetectorComponent> ent, ref UseInHandEvent args)
    {
        if (!ent.Comp.HandToggleable)
            return;

        args.Handled = true;
        Toggle(ent);

        _audio.PlayPredicted(ent.Comp.ToggleSound, ent, args.User);
    }

    private void OnMotionDetectorGetVerbs(Entity<MotionDetectorComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
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
                _popup.PopupClient($"You change the {Name(ent)} to {(ent.Comp.Short ? "short" : "long")} range mode", ent, user);
            },
        });
    }

    private void OnMotionDetectorDropped<T>(Entity<MotionDetectorComponent> ent, ref T args)
    {
        if (!ent.Comp.DeactivateOnDrop)
            return;

        ent.Comp.Enabled = false;
        Dirty(ent);
        UpdateAppearance(ent);
        MotionDetectorUpdated(ent);
    }

    private void OnMotionDetectorExamined(Entity<MotionDetectorComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(MotionDetectorComponent)))
        {
            var mode = ent.Comp.Short ? "short" : "long";
            args.PushMarkup($"The motion detector is in [color=cyan]{mode}[/color] scanning mode.");
        }
    }

    private void OnGetItemActions(Entity<ToggleableMotionDetectorComponent> ent, ref GetItemActionsEvent args)
    {
        if (ent.Comp.Slots != SlotFlags.All &&
            (args.InHands ||
            (args.SlotFlags & ent.Comp.Slots) == 0))
        {
            return;
        }

        args.AddAction(ref ent.Comp.Action, ent.Comp.ActionId);
        Dirty(ent);
    }

    private void OnToggleAction(Entity<ToggleableMotionDetectorComponent> ent, ref ToggleableMotionDetectorActionEvent args)
    {
        var user = args.Performer;
        if (TryComp(ent, out MotionDetectorComponent? detector))
            _motionDetector.Toggle((ent, detector));

        _audio.PlayPredicted(ent.Comp.ToggleSound, ent, user);
        DetectorUpdated(ent);

        var popup = _motionDetector.IsEnabled((ent, detector))
            ? Loc.GetString("rmc-toggleable-motion-detector-on", ("gun", ent))
            : Loc.GetString("rmc-toggleable-motion-detector-off", ("gun", ent));
        _popup.PopupClient(popup, user, user, PopupType.Large);
    }

    private void OnGetBatteryDrain(Entity<ToggleableMotionDetectorComponent> ent, ref GunGetBatteryDrainEvent args)
    {
        if (_motionDetector.IsEnabled(ent.Owner))
            args.Drain += ent.Comp.BatteryDrain;
    }

    private void OnGunUnpowered(Entity<ToggleableMotionDetectorComponent> ent, ref GunUnpoweredEvent args)
    {
        if (!TryComp(ent, out MotionDetectorComponent? detector))
            return;

        _motionDetector.Disable((ent, detector));
        DetectorUpdated(ent);
    }

    private void OnMotionDetectorUpdated(Entity<ToggleableMotionDetectorComponent> ent, ref MotionDetectorUpdatedEvent args)
    {
        DetectorUpdated(ent);
    }

    private void DetectorUpdated(Entity<ToggleableMotionDetectorComponent> ent)
    {
        var enabled = false;
        if (TryComp(ent, out MotionDetectorComponent? detector))
            enabled = _motionDetector.IsEnabled((ent, detector));

        _rmcGunBattery.RefreshBatteryDrain(ent.Owner);
        _actions.SetToggled(ent.Comp.Action, enabled);
    }

    private void OnMotionDetectorTracked(Entity<MotionDetectorTrackedComponent> ent, ref MoveEvent args)
    {
        _toUpdate.Add(ent);
    }

    private void UpdateAppearance(Entity<MotionDetectorComponent> ent)
    {
        _appearance.SetData(ent, MotionDetectorLayer.Setting, ent.Comp.Short ? MotionDetectorSetting.Short : MotionDetectorSetting.Long);

        var count = Math.Min(ent.Comp.Blips.Count, 9);
        if (!ent.Comp.Enabled)
            count = -1;

        _appearance.SetData(ent, MotionDetectorLayer.Number, count);
    }

    private void DisableMotionDetectors(EntityUid ent)
    {
        if (_detectorQuery.TryComp(ent, out var detector))
        {
            detector.Enabled = false;
            Dirty(ent, detector);
            UpdateAppearance((ent, detector));
            MotionDetectorUpdated((ent, detector));
        }

        if (_storageQuery.TryComp(ent, out var storage))
        {
            foreach (var stored in storage.StoredItems.Keys)
            {
                DisableMotionDetectors(stored);
            }
        }
    }

    private TimeSpan GetRefreshRate(Entity<MotionDetectorComponent> ent)
    {
        return ent.Comp.Short ? ent.Comp.ShortRefresh : ent.Comp.LongRefresh;
    }

    private void MotionDetectorUpdated(Entity<MotionDetectorComponent> ent)
    {
        var ev = new MotionDetectorUpdatedEvent(ent.Comp.Enabled);
        RaiseLocalEvent(ent, ref ev);
    }

    public void Toggle(Entity<MotionDetectorComponent> ent)
    {
        ref var enabled = ref ent.Comp.Enabled;
        enabled = !enabled;

        if (enabled)
            ent.Comp.NextScanAt = _timing.CurTime + GetRefreshRate(ent);

        ent.Comp.Blips.Clear();
        Dirty(ent);
        UpdateAppearance(ent);
        MotionDetectorUpdated(ent);
    }

    public void Disable(Entity<MotionDetectorComponent> ent)
    {
        if (!ent.Comp.Enabled)
            return;

        Toggle(ent);
    }

    public bool IsEnabled(Entity<MotionDetectorComponent?> ent)
    {
        return Resolve(ent, ref ent.Comp, false) && ent.Comp.Enabled;
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        try
        {
            foreach (var update in _toUpdate)
            {
                update.Comp.LastMove = time;
            }
        }
        finally
        {
            _toUpdate.Clear();
        }

        var detectors = EntityQueryEnumerator<MotionDetectorComponent>();
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
            _entityLookup.GetEntitiesInRange(uid.ToCoordinates(), range, _tracked, LookupFlags.Uncontained);

            detector.Blips.Clear();
            foreach (var tracked in _tracked)
            {
                if (tracked.Comp.LastMove < time - detector.MoveTime)
                    continue;

                detector.Blips.Add(_transform.GetMapCoordinates(tracked));
            }

            UpdateAppearance((uid, detector));
            if (detector.Blips.Count == 0)
            {
                _audio.PlayPvs(detector.ScanEmptySound, uid);
                continue;
            }

            _audio.PlayPvs(detector.ScanSound, uid);
        }
    }
}
