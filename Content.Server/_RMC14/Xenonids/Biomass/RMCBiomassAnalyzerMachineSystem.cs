using Content.Server.Power.EntitySystems;
using Content.Shared._RMC14.Armor.Plates;
using Content.Shared._RMC14.Xenonids.Biomass;
using Content.Shared.Containers.ItemSlots;
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
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCBiomassAnalyzerMachineComponent, InteractUsingEvent>(OnInteractUsing, before: [typeof(ItemSlotsSystem)]);
        SubscribeLocalEvent<RMCBiomassAnalyzerMachineComponent, BeforeActivatableUIOpenEvent>(OnBeforeUIOpen);
        SubscribeLocalEvent<RMCBiomassAnalyzerMachineComponent, EntInsertedIntoContainerMessage>(OnOrganInserted);
        SubscribeLocalEvent<RMCBiomassAnalyzerMachineComponent, EntRemovedFromContainerMessage>(OnOrganRemoved);

        Subs.BuiEvents<RMCBiomassAnalyzerMachineComponent>(RMCBiomassAnalyzerUi.Key,
            subs =>
            {
                subs.Event<RMCBiomassAnalyzerEjectOrganBuiMsg>(OnEjectOrganMsg);
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

        if (!this.IsPowered(ent, EntityManager))
        {
            _popup.PopupCursor("This machine has no power!", args.User);
            args.Handled = true;
            return;
        }

        if (isPlate)
        {
            args.Handled = true;
            _biomass.AddPoints(recyclable!.RecyclableValue);
            _popup.PopupCursor($"You recycle the plate for {recyclable.RecyclableValue} biomass points.", args.User);
            _audio.PlayPvs(ent.Comp.NotifySound, ent);
            QueueDel(args.Used);
            return;
        }

        if (_itemSlots.GetItemOrNull(ent, ent.Comp.OrganSlotId) != null)
        {
            _popup.PopupCursor("The analyzer's receptacle is already occupied!", args.User);
            args.Handled = true;
        }
    }

    private void OnOrganInserted(Entity<RMCBiomassAnalyzerMachineComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.OrganSlotId)
            return;

        Dirty(ent);
        if (ent.Comp.AutoProcessOrgan)
            TryStartProcessing(ent, null);
    }

    private void OnOrganRemoved(Entity<RMCBiomassAnalyzerMachineComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID == ent.Comp.OrganSlotId)
            Dirty(ent);
    }

    private void OnEjectOrganMsg(Entity<RMCBiomassAnalyzerMachineComponent> ent, ref RMCBiomassAnalyzerEjectOrganBuiMsg args)
    {
        if (!_itemSlots.TryGetSlot(ent, ent.Comp.OrganSlotId, out var slot) || slot.Item == null)
            return;

        if (ent.Comp.PrintingUntil != null)
        {
            _popup.PopupEntity("The analyzer is busy!", ent, args.Actor);
            return;
        }

        if (_itemSlots.TryEjectToHands(ent, slot, args.Actor))
            _popup.PopupEntity("You eject the specimen from the analyzer.", ent, args.Actor);
    }

    private void OnToggleAutoProcessMsg(Entity<RMCBiomassAnalyzerMachineComponent> ent, ref RMCBiomassAnalyzerToggleAutoProcessBuiMsg args)
    {
        ent.Comp.AutoProcessOrgan = !ent.Comp.AutoProcessOrgan;
        Dirty(ent);

        if (ent.Comp.AutoProcessOrgan)
            TryStartProcessing(ent, args.Actor);
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
            if (ent.Comp.PrintQueue.Count >= ent.Comp.MaxQueueLength)
            {
                _popup.PopupEntity("The analyzer's print queue is full!", ent, args.Actor);
                break;
            }

            ent.Comp.PrintQueue.Add(args.UpgradeId);
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
        ent.Comp.QueueProcessing = !ent.Comp.QueueProcessing;
        if (ent.Comp.QueueProcessing)
            ent.Comp.NextQueueItemAt = null;

        Dirty(ent);
    }

    private void OnRemoveFromQueueMsg(Entity<RMCBiomassAnalyzerMachineComponent> ent, ref RMCBiomassAnalyzerRemoveFromQueueBuiMsg args)
    {
        if (args.Index < 0 || args.Index >= ent.Comp.PrintQueue.Count)
            return;

        ent.Comp.PrintQueue.RemoveAt(args.Index);
        Dirty(ent);
    }

    private bool TryStartProcessing(Entity<RMCBiomassAnalyzerMachineComponent> ent, EntityUid? actor)
    {
        if (ent.Comp.PrintingUntil != null)
            return false;

        if (_itemSlots.GetItemOrNull(ent, ent.Comp.OrganSlotId) is not { } organ ||
            !TryComp(organ, out RMCBiomassOrganComponent? organComp))
        {
            return false;
        }

        ent.Comp.ProcessingValue = organComp.Value;
        QueueDel(organ);
        StartPrintingAnimation(ent);

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
        if (ent.Comp.ProcessingValue > 0)
        {
            _biomass.AddPoints(ent.Comp.ProcessingValue);
            ent.Comp.ProcessingValue = 0;
            Dirty(ent);
        }

        if (ent.Comp.AutoProcessOrgan)
            TryStartProcessing(ent, null);
    }

    private void ProcessNextQueueItem(Entity<RMCBiomassAnalyzerMachineComponent> ent)
    {
        if (ent.Comp.PrintQueue.Count == 0)
        {
            ent.Comp.QueueProcessing = false;
            Dirty(ent);
            return;
        }

        var upgradeId = ent.Comp.PrintQueue[0];
        var spawnAt = Transform(ent.Owner).Coordinates;
        if (!_biomass.TryPurchaseUpgrade(upgradeId, spawnAt, out _))
        {
            ent.Comp.QueueProcessing = false;
            Dirty(ent);
            _popup.PopupEntity("The analyzer flashes red - insufficient biomass or clearance to continue printing!", ent);
            return;
        }

        ent.Comp.PrintQueue.RemoveAt(0);
        ent.Comp.NextQueueItemAt = Timing.CurTime + ent.Comp.QueuePrintDelay;

        if (ent.Comp.PrintQueue.Count == 0)
            ent.Comp.QueueProcessing = false;

        Dirty(ent);
        _audio.PlayPvs(ent.Comp.NotifySound, ent);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<RMCBiomassAnalyzerMachineComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            var ent = (Entity<RMCBiomassAnalyzerMachineComponent>) (uid, comp);

            if (comp.PrintingUntil is { } printingUntil && Timing.CurTime >= printingUntil)
            {
                comp.PrintingUntil = null;
                Dirty(uid, comp);
                UpdateAppearance(ent);
                FinishProcessingOrgan(ent);
            }

            if (comp.QueueProcessing && comp.PrintQueue.Count > 0 &&
                (comp.NextQueueItemAt is not { } nextAt || Timing.CurTime >= nextAt))
            {
                ProcessNextQueueItem(ent);
            }
        }
    }
}
