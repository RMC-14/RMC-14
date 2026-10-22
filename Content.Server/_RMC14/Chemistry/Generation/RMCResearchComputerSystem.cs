using Content.Server.Power.EntitySystems;
using Content.Shared._RMC14.Chemistry.Generation;
using Content.Shared._RMC14.Intel;
using Content.Shared.Interaction;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;

namespace Content.Server._RMC14.Chemistry.Generation;

public sealed class RMCResearchComputerSystem : SharedRMCResearchComputerSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly RMCChemistryResearchSystem _research = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCResearchComputerComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<RMCResearchComputerComponent, BeforeActivatableUIOpenEvent>(OnBeforeUIOpen);

        Subs.BuiEvents<RMCResearchComputerComponent>(RMCResearchComputerUi.Key,
            subs =>
            {
                subs.Event<RMCResearchComputerBrokerClearanceBuiMsg>(OnBrokerClearance);
                subs.Event<RMCResearchComputerRequestXAccessBuiMsg>(OnRequestXAccess);
                subs.Event<RMCResearchComputerTakeContractBuiMsg>(OnTakeContract);
                subs.Event<RMCResearchComputerReprintContractBuiMsg>(OnReprintContract);
                subs.Event<RMCResearchComputerReadDocumentBuiMsg>(OnReadDocument);
                subs.Event<RMCResearchComputerPrintDocumentBuiMsg>(OnPrintDocument);
                subs.Event<RMCResearchComputerPublishDocumentBuiMsg>(OnPublishDocument);
                subs.Event<RMCResearchComputerUnpublishDocumentBuiMsg>(OnUnpublishDocument);
                subs.Event<RMCResearchComputerAnnounceDocumentBuiMsg>(OnAnnounceDocument);
            });
    }

    private void OnBeforeUIOpen(Entity<RMCResearchComputerComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        _research.EnsureResearch();
    }

    private void OnInteractUsing(Entity<RMCResearchComputerComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        var isRerollDisk = HasComp<RMCResearchRerollDiskComponent>(args.Used);
        if (!isRerollDisk && !HasComp<PaperComponent>(args.Used))
            return;

        if (!this.IsPowered(ent, EntityManager))
        {
            _popup.PopupCursor("This terminal has no power!", args.User);
            args.Handled = true;
            return;
        }

        var notifySound = _research.EnsureResearch().Comp.NotifySound;

        if (isRerollDisk)
        {
            _research.ForceRerollContracts();
            QueueDel(args.Used);
            _popup.PopupCursor("The terminal accepts the disk, and rerolls the contract chemicals.", args.User);
            _audio.PlayPvs(notifySound, ent);
            args.Handled = true;
            return;
        }

        if (TryComp<IntelChemGrantPaperComponent>(args.Used, out var grant))
        {
            _research.AddCredits(grant.Grant);
            QueueDel(args.Used);
            _popup.PopupCursor($"Research grant redeemed for {grant.Grant} credits.", args.User);
            _audio.PlayPvs(notifySound, ent);
            args.Handled = true;
            return;
        }

        if (!_research.TrySaveManualScan(args.Used))
            return;

        _popup.PopupCursor("The terminal scans the document and files it under Manual Scans.", args.User);
        args.Handled = true;
    }

    private void OnBrokerClearance(Entity<RMCResearchComputerComponent> ent, ref RMCResearchComputerBrokerClearanceBuiMsg args)
    {
        _research.TryRaiseClearance(Transform(ent).Coordinates);
    }

    private void OnRequestXAccess(Entity<RMCResearchComputerComponent> ent, ref RMCResearchComputerRequestXAccessBuiMsg args)
    {
        _research.TryRequestXAccess();
    }

    private void OnTakeContract(Entity<RMCResearchComputerComponent> ent, ref RMCResearchComputerTakeContractBuiMsg args)
    {
        _research.TryTakeContract(args.Index, Transform(ent).Coordinates);
    }

    private void OnReprintContract(Entity<RMCResearchComputerComponent> ent, ref RMCResearchComputerReprintContractBuiMsg args)
    {
        if (TryStartPrint(ent))
            _research.TryReprintContract(Transform(ent).Coordinates);
    }

    private void OnReadDocument(Entity<RMCResearchComputerComponent> ent, ref RMCResearchComputerReadDocumentBuiMsg args)
    {
        if (TryStartPrint(ent))
            _research.TryOpenReadingCopy(args.Id, Transform(ent).Coordinates, args.Actor);
    }

    private void OnPrintDocument(Entity<RMCResearchComputerComponent> ent, ref RMCResearchComputerPrintDocumentBuiMsg args)
    {
        if (TryStartPrint(ent))
            _research.TryPrintDocument(args.Id, Transform(ent).Coordinates);
    }

    private void OnPublishDocument(Entity<RMCResearchComputerComponent> ent, ref RMCResearchComputerPublishDocumentBuiMsg args)
    {
        _research.PublishDocument(args.Id);
    }

    private void OnUnpublishDocument(Entity<RMCResearchComputerComponent> ent, ref RMCResearchComputerUnpublishDocumentBuiMsg args)
    {
        _research.UnpublishDocument(args.Id);
    }

    private void OnAnnounceDocument(Entity<RMCResearchComputerComponent> ent, ref RMCResearchComputerAnnounceDocumentBuiMsg args)
    {
        _research.AnnounceDocument(ent.Owner, args.Id, args.Actor);
    }
}
