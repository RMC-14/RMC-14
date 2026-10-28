using Content.Shared._RMC14.Armor.Plates;
using Content.Shared._RMC14.Xenonids.Biomass;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;

namespace Content.Server._RMC14.Xenonids.Biomass;

public sealed class RMCBiomassAnalyzerMachineSystem : SharedRMCBiomassAnalyzerMachineSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly RMCBiomassAnalyzerDataSystem _biomass = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCBiomassAnalyzerMachineComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<RMCBiomassAnalyzerMachineComponent, BeforeActivatableUIOpenEvent>(OnBeforeUIOpen);
        SubscribeLocalEvent<RMCBiomassAnalyzerMachineComponent, EntRemovedFromContainerMessage>(OnOrganRemoved);

        Subs.BuiEvents<RMCBiomassAnalyzerMachineComponent>(RMCBiomassAnalyzerUi.Key,
            subs =>
            {
                subs.Event<RMCBiomassAnalyzerEjectOrganBuiMsg>(OnEjectOrganMsg);
                subs.Event<RMCBiomassAnalyzerProcessOrganBuiMsg>(OnProcessOrganMsg);
                subs.Event<RMCBiomassAnalyzerToggleAutoProcessBuiMsg>(OnToggleAutoProcessMsg);
                subs.Event<RMCBiomassAnalyzerEnqueuePrintBuiMsg>(OnEnqueuePrintMsg);
                subs.Event<RMCBiomassAnalyzerToggleQueueBuiMsg>(OnToggleQueueMsg);
                subs.Event<RMCBiomassAnalyzerRemoveFromQueueBuiMsg>(OnRemoveFromQueueMsg);
            });
    }

    private void OnBeforeUIOpen(Entity<RMCBiomassAnalyzerMachineComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        _biomass.EnsureAnalyzer();
    }

    private void OnInteractUsing(Entity<RMCBiomassAnalyzerMachineComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        var isPlate = TryComp<RMCEmergencyInjectorPlateComponent>(args.Used, out var recyclable) && recyclable.RecyclableValue > 0;
        var isOrgan = HasComp<RMCBiomassOrganComponent>(args.Used);
        if (!isPlate && !isOrgan)
            return;

        args.Handled = true;
        if (!IsMachinePowered(ent))
        {
            _popup.PopupCursor("This machine has no power!", args.User);
            return;
        }

        if (isPlate)
        {
            _biomass.AddPoints(recyclable!.RecyclableValue);
            _popup.PopupCursor($"You recycle the plate for {recyclable.RecyclableValue} biomass points.", args.User);
            _audio.PlayPvs(ent.Comp.NotifySound, ent);
            QueueDel(args.Used);
            return;
        }

        TryQueueOrgan(ent, args.Used, args.User);
    }

    private void TryQueueOrgan(Entity<RMCBiomassAnalyzerMachineComponent> ent, EntityUid organ, EntityUid user)
    {
        if (ent.Comp.Queue.Count >= ent.Comp.MaxQueueLength)
        {
            _popup.PopupCursor("The analyzer's queue is full!", user);
            return;
        }

        var container = _container.EnsureContainer<Container>(ent, ent.Comp.OrganContainerId);
        if (!_hands.TryDropIntoContainer(user, organ, container))
            return;

        ent.Comp.Queue.Add(new RMCBiomassQueueJob(null, GetNetEntity(organ)));
        if (ent.Comp.AutoProcessOrgan && !ent.Comp.QueueProcessing)
        {
            ent.Comp.QueueProcessing = true;
            ent.Comp.NextQueueItemAt = null;
        }

        Dirty(ent);
        _popup.PopupCursor("You add the specimen to the analyzer's queue.", user);
        _audio.PlayPvs(ent.Comp.InsertSound, ent);
    }

    private void OnOrganRemoved(Entity<RMCBiomassAnalyzerMachineComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.OrganContainerId)
            return;

        RemoveOrganJob(ent, args.Entity);
    }

    private void RemoveOrganJob(Entity<RMCBiomassAnalyzerMachineComponent> ent, EntityUid organ)
    {
        var netOrgan = GetNetEntity(organ);
        if (ent.Comp.Queue.RemoveAll(job => job.Organ == netOrgan) == 0)
            return;

        if (ent.Comp.Queue.Count == 0)
            ent.Comp.QueueProcessing = false;

        Dirty(ent);
    }

    private void EjectOrgan(Entity<RMCBiomassAnalyzerMachineComponent> ent, EntityUid organ, EntityUid user)
    {
        if (!_container.TryGetContainer(ent, ent.Comp.OrganContainerId, out var container) ||
            !_container.Remove(organ, container))
        {
            return;
        }

        _hands.PickupOrDrop(user, organ);
        _popup.PopupEntity("You eject the specimen from the analyzer.", ent, user);
        _audio.PlayPvs(ent.Comp.EjectSound, ent);
    }

    private void OnEjectOrganMsg(Entity<RMCBiomassAnalyzerMachineComponent> ent, ref RMCBiomassAnalyzerEjectOrganBuiMsg args)
    {
        if (GetFirstQueuedOrgan(ent) is { } organ)
            EjectOrgan(ent, organ, args.Actor);
    }

    private void OnProcessOrganMsg(Entity<RMCBiomassAnalyzerMachineComponent> ent, ref RMCBiomassAnalyzerProcessOrganBuiMsg args)
    {
        if (ent.Comp.PrintingUntil != null)
        {
            _popup.PopupEntity("The analyzer is busy!", ent, args.Actor);
            return;
        }

        if (GetFirstQueuedOrgan(ent) is { } organ)
            TryStartProcessing(ent, organ, args.Actor);
    }

    private void OnToggleAutoProcessMsg(Entity<RMCBiomassAnalyzerMachineComponent> ent, ref RMCBiomassAnalyzerToggleAutoProcessBuiMsg args)
    {
        ent.Comp.AutoProcessOrgan = !ent.Comp.AutoProcessOrgan;
        Dirty(ent);
    }

    private void OnEnqueuePrintMsg(Entity<RMCBiomassAnalyzerMachineComponent> ent, ref RMCBiomassAnalyzerEnqueuePrintBuiMsg args)
    {
        if (args.Amount <= 0)
            return;

        if (!_biomass.TryGetUpgrade(args.UpgradeId, out var upgrade))
            return;

        if (!_biomass.HasSufficientClearance(upgrade))
        {
            _popup.PopupEntity("You don't have clearance for this upgrade!", ent, args.Actor);
            return;
        }

        var added = 0;
        for (var i = 0; i < args.Amount; i++)
        {
            if (ent.Comp.Queue.Count >= ent.Comp.MaxQueueLength)
            {
                _popup.PopupEntity("The analyzer's queue is full!", ent, args.Actor);
                break;
            }

            ent.Comp.Queue.Add(new RMCBiomassQueueJob(args.UpgradeId, null));
            added++;
        }

        if (added == 0)
            return;

        if (!ent.Comp.QueueProcessing)
        {
            ent.Comp.QueueProcessing = true;
            ent.Comp.NextQueueItemAt = Timing.CurTime + ent.Comp.QueuePrintDelay;
        }

        Dirty(ent);
    }

    private void OnToggleQueueMsg(Entity<RMCBiomassAnalyzerMachineComponent> ent, ref RMCBiomassAnalyzerToggleQueueBuiMsg args)
    {
        if (!ent.Comp.QueueProcessing && ent.Comp.Queue.Count == 0)
            return;

        if (!ent.Comp.QueueProcessing && !IsMachinePowered(ent))
        {
            _popup.PopupEntity("This machine has no power!", ent, args.Actor);
            return;
        }

        ent.Comp.QueueProcessing = !ent.Comp.QueueProcessing;
        if (ent.Comp.QueueProcessing)
            ent.Comp.NextQueueItemAt = null;

        Dirty(ent);
    }

    private void OnRemoveFromQueueMsg(Entity<RMCBiomassAnalyzerMachineComponent> ent, ref RMCBiomassAnalyzerRemoveFromQueueBuiMsg args)
    {
        if (args.Index < 0 || args.Index >= ent.Comp.Queue.Count)
            return;

        if (ent.Comp.Queue[args.Index].Organ is { } netOrgan)
        {
            if (TryGetEntity(netOrgan, out var organ))
                EjectOrgan(ent, organ.Value, args.Actor);

            return;
        }

        ent.Comp.Queue.RemoveAt(args.Index);
        if (ent.Comp.Queue.Count == 0)
            ent.Comp.QueueProcessing = false;

        Dirty(ent);
    }

    private bool TryStartProcessing(Entity<RMCBiomassAnalyzerMachineComponent> ent, EntityUid organ, EntityUid? actor)
    {
        if (ent.Comp.PrintingUntil != null || !IsMachinePowered(ent))
            return false;

        if (!TryComp(organ, out RMCBiomassOrganComponent? organComp))
            return false;

        RemoveOrganJob(ent, organ);
        ent.Comp.ProcessingValue = organComp.Value;
        QueueDel(organ);
        StartPrintingAnimation(ent, ent.Comp.PrintingDuration);

        var message = $"The analyzer processes the specimen, extracting {organComp.Value} biomass points.";
        if (actor is { } user)
            _popup.PopupEntity(message, ent, user);
        else
            _popup.PopupEntity(message, ent);

        _audio.PlayPvs(ent.Comp.NotifySound, ent);
        return true;
    }

    private void FinishProcessingOrgan(Entity<RMCBiomassAnalyzerMachineComponent> ent)
    {
        if (ent.Comp.ProcessingValue <= 0)
            return;

        _biomass.AddPoints(ent.Comp.ProcessingValue);
        ent.Comp.ProcessingValue = 0;
        Dirty(ent);
    }

    private void ProcessNextQueueItem(Entity<RMCBiomassAnalyzerMachineComponent> ent)
    {
        if (ent.Comp.Queue.Count == 0)
        {
            ent.Comp.QueueProcessing = false;
            Dirty(ent);
            return;
        }

        var job = ent.Comp.Queue[0];
        if (job.Organ is { } netOrgan)
        {
            if (!TryGetEntity(netOrgan, out var organ) || !TryStartProcessing(ent, organ.Value, null))
            {
                ent.Comp.Queue.RemoveAt(0);
                Dirty(ent);
            }

            return;
        }

        if (job.Upgrade is not { } upgradeId)
        {
            ent.Comp.Queue.RemoveAt(0);
            Dirty(ent);
            return;
        }

        var spawnAt = Transform(ent.Owner).Coordinates;
        if (!_biomass.TryPurchaseUpgrade(upgradeId, spawnAt, out _))
        {
            ent.Comp.QueueProcessing = false;
            Dirty(ent);
            _popup.PopupEntity("The analyzer flashes red - insufficient biomass or clearance to continue printing!", ent);
            return;
        }

        ent.Comp.Queue.RemoveAt(0);
        ent.Comp.NextQueueItemAt = Timing.CurTime + ent.Comp.QueuePrintDelay;

        if (ent.Comp.Queue.Count == 0)
            ent.Comp.QueueProcessing = false;

        StartPrintingAnimation(ent, ent.Comp.QueuePrintDelay);
        _audio.PlayPvs(ent.Comp.NotifySound, ent);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<RMCBiomassAnalyzerMachineComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            var ent = (Entity<RMCBiomassAnalyzerMachineComponent>) (uid, comp);
            UpdateAppearance(ent);

            if (!IsMachinePowered(uid))
            {
                if (comp.PrintingUntil != null)
                    comp.PrintingUntil += TimeSpan.FromSeconds(frameTime);

                if (comp.QueueProcessing)
                {
                    comp.QueueProcessing = false;
                    Dirty(uid, comp);
                    _popup.PopupEntity("The analyzer loses power, halting the queue!", uid, PopupType.SmallCaution);
                }

                continue;
            }

            if (comp.PrintingUntil is { } printingUntil && Timing.CurTime >= printingUntil)
            {
                comp.PrintingUntil = null;
                Dirty(uid, comp);
                UpdateAppearance(ent);
                FinishProcessingOrgan(ent);
            }

            if (comp.PrintingUntil == null && comp.QueueProcessing && comp.Queue.Count > 0 &&
                (comp.NextQueueItemAt is not { } nextAt || Timing.CurTime >= nextAt))
            {
                ProcessNextQueueItem(ent);
            }
        }
    }
}
