using System.Linq;
using Content.Server.Power.EntitySystems;
using Content.Shared._RMC14.Chemistry.Generation;
using Content.Shared._RMC14.Dialog;
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
    [Dependency] private readonly DialogSystem _dialog = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly RMCChemistryResearchSystem _research = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCResearchComputerComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<RMCResearchComputerComponent, BeforeActivatableUIOpenEvent>(OnBeforeUIOpen);
        SubscribeLocalEvent<RMCResearchComputerComponent, RMCResearchComputerScanConfirmEvent>(OnScanConfirm);
        SubscribeLocalEvent<RMCResearchComputerComponent, RMCResearchComputerScanExistingCategoryEvent>(OnScanExistingCategory);
        SubscribeLocalEvent<RMCResearchComputerComponent, RMCResearchComputerScanNewCategoryEvent>(OnScanNewCategory);
        SubscribeLocalEvent<RMCResearchComputerComponent, RMCResearchComputerScanCategoryEvent>(OnScanCategory);
        SubscribeLocalEvent<RMCResearchComputerComponent, RMCResearchComputerScanCategoryInputEvent>(OnScanCategoryInput);
        SubscribeLocalEvent<RMCResearchComputerComponent, RMCResearchComputerAnnounceInputEvent>(OnAnnounceInput);

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

        if (MetaData(args.Used).EntityPrototype == null)
            return;

        var ev = new RMCResearchComputerScanConfirmEvent(GetNetEntity(args.User), GetNetEntity(args.Used));
        _dialog.OpenConfirmation(ent, args.User, Name(ent), $"Do you want to save {Name(args.Used)} to the research database?", ev);
        args.Handled = true;
    }

    private void OnScanConfirm(Entity<RMCResearchComputerComponent> ent, ref RMCResearchComputerScanConfirmEvent args)
    {
        if (!TryGetEntity(args.User, out var user))
            return;

        var options = new List<DialogOption>
        {
            new("Existing", new RMCResearchComputerScanExistingCategoryEvent(args.User, args.Paper)),
            new("New", new RMCResearchComputerScanNewCategoryEvent(args.User, args.Paper)),
        };
        _dialog.OpenOptions(ent, user.Value, Name(ent), options, "Use existing or new category?");
    }

    private void OnScanExistingCategory(Entity<RMCResearchComputerComponent> ent, ref RMCResearchComputerScanExistingCategoryEvent args)
    {
        if (!TryGetEntity(args.User, out var user))
            return;

        var research = _research.EnsureResearch();
        var categories = research.Comp.Documents.Keys.Order().ToList();
        if (categories.Count == 0)
        {
            SaveManualScan(ent, args.User, args.Paper, research.Comp.DefaultScanCategory);
            return;
        }

        var options = new List<DialogOption>();
        foreach (var category in categories)
        {
            options.Add(new DialogOption(category, new RMCResearchComputerScanCategoryEvent(args.User, args.Paper, category)));
        }

        _dialog.OpenOptions(ent, user.Value, "Categories", options, "Select a category:");
    }

    private void OnScanNewCategory(Entity<RMCResearchComputerComponent> ent, ref RMCResearchComputerScanNewCategoryEvent args)
    {
        if (!TryGetEntity(args.User, out var user))
            return;

        var ev = new RMCResearchComputerScanCategoryInputEvent(args.User, args.Paper);
        var limit = _research.EnsureResearch().Comp.CategoryCharacterLimit;
        _dialog.OpenInput(ent, user.Value, "Please enter the category of the paper:", ev, characterLimit: limit);
    }

    private void OnScanCategory(Entity<RMCResearchComputerComponent> ent, ref RMCResearchComputerScanCategoryEvent args)
    {
        SaveManualScan(ent, args.User, args.Paper, args.Category);
    }

    private void OnScanCategoryInput(Entity<RMCResearchComputerComponent> ent, ref RMCResearchComputerScanCategoryInputEvent args)
    {
        SaveManualScan(ent, args.User, args.Paper, args.Message);
    }

    private void SaveManualScan(Entity<RMCResearchComputerComponent> ent, NetEntity netUser, NetEntity netPaper, string category)
    {
        if (!TryGetEntity(netUser, out var user) ||
            !TryGetEntity(netPaper, out var paper) ||
            TerminatingOrDeleted(paper))
        {
            return;
        }

        category = category.Trim();
        if (category.Length == 0)
            category = _research.EnsureResearch().Comp.DefaultScanCategory;

        if (!_research.TrySaveManualScan(paper.Value, category))
            return;

        _popup.PopupEntity($"The terminal scans the document and files it under {category}.", ent, user.Value);
    }

    private void OnBrokerClearance(Entity<RMCResearchComputerComponent> ent, ref RMCResearchComputerBrokerClearanceBuiMsg args)
    {
        if (_research.TryRaiseClearance(Transform(ent).Coordinates))
            return;

        var research = _research.EnsureResearch().Comp;
        var message = research.ClearanceLevel >= research.MaxClearanceLevel
            ? "Clearance is already at the maximum level."
            : $"Not enough research credits. Increasing the clearance costs {_research.GetClearanceUpgradeCost(research)} credits.";
        _popup.PopupEntity(message, ent, args.Actor, PopupType.SmallCaution);
    }

    private void OnRequestXAccess(Entity<RMCResearchComputerComponent> ent, ref RMCResearchComputerRequestXAccessBuiMsg args)
    {
        if (_research.TryRequestXAccess())
        {
            _popup.PopupEntity("Clearance Level X Acquired.", ent, PopupType.Medium);
            return;
        }

        var research = _research.EnsureResearch().Comp;
        string message;
        if (research.ReachedXAccess)
            message = "5X access has already been granted.";
        else if (research.ClearanceLevel < research.MaxClearanceLevel)
            message = $"Clearance level {research.MaxClearanceLevel} is required to request 5X access.";
        else
            message = $"Not enough research credits. 5X access costs {research.XAccessCost} credits.";

        _popup.PopupEntity(message, ent, args.Actor, PopupType.SmallCaution);
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
        _research.TrySendDocumentContent(ent, args.Id, args.Actor);
    }

    private void OnPrintDocument(Entity<RMCResearchComputerComponent> ent, ref RMCResearchComputerPrintDocumentBuiMsg args)
    {
        if (TryStartPrint(ent))
            _research.TryPrintDocument(args.Id, Transform(ent).Coordinates);
    }

    private void OnPublishDocument(Entity<RMCResearchComputerComponent> ent, ref RMCResearchComputerPublishDocumentBuiMsg args)
    {
        if (ent.Comp.MainTerminal)
            _research.PublishDocument(args.Id);
    }

    private void OnUnpublishDocument(Entity<RMCResearchComputerComponent> ent, ref RMCResearchComputerUnpublishDocumentBuiMsg args)
    {
        if (ent.Comp.MainTerminal)
            _research.UnpublishDocument(args.Id);
    }

    private void OnAnnounceDocument(Entity<RMCResearchComputerComponent> ent, ref RMCResearchComputerAnnounceDocumentBuiMsg args)
    {
        _research.RequestAnnouncement(ent, args.Id, args.Actor);
    }

    private void OnAnnounceInput(Entity<RMCResearchComputerComponent> ent, ref RMCResearchComputerAnnounceInputEvent args)
    {
        if (TryGetEntity(args.User, out var user))
            _research.AnnounceDocument(ent, args.Id, user.Value, args.Message);
    }
}
