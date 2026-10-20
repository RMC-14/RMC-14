using System.Linq;
using Content.Client._RMC14.UserInterface;
using Content.Client.Resources;
using Content.Shared._RMC14.Chemistry.Generation;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared._RMC14.UserInterface;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using static Content.Client._RMC14.Chemistry.Generation.RMCResearchComputerWindow;

namespace Content.Client._RMC14.Chemistry.Generation;

[UsedImplicitly]
public sealed class RMCResearchComputerBui : BoundUserInterface, IRefreshableBui
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly RMCReagentSystem _rmcReagent;
    private readonly SharedRMCChemistryResearchSystem _research;

    private RMCResearchComputerWindow? _window;
    private ConfirmationWindow? _confirm;

    private enum SortColumn
    {
        Time,
        Compound,
    }

    private SortColumn _documentsSort = SortColumn.Time;
    private bool _documentsSortAscending;
    private SortColumn _publishedSort = SortColumn.Time;
    private bool _publishedSortAscending;

    public RMCResearchComputerBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
        _rmcReagent = EntMan.System<RMCReagentSystem>();
        _research = EntMan.System<SharedRMCChemistryResearchSystem>();
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<RMCResearchComputerWindow>();

        _window.BrokerClearanceButton.OnPressed += _ =>
        {
            if (_research.TryGetResearch(out var research) &&
                research.Value.Comp.ClearanceLevel >= research.Value.Comp.MaxClearanceLevel)
                OnRequestXAccessPressed();
            else
                OnBrokerClearancePressed();
        };
        _window.ReprintContractButton.OnPressed += _ => SendMessage(new RMCResearchComputerReprintContractBuiMsg());
        _window.HideOldReportsButton.OnPressed += _ => Refresh();

        _window.DocumentsSortTimeButton.OnPressed += _ => SetSort(SortColumn.Time, ref _documentsSort, ref _documentsSortAscending);
        _window.DocumentsSortCompoundButton.OnPressed += _ => SetSort(SortColumn.Compound, ref _documentsSort, ref _documentsSortAscending);
        _window.PublishedSortTimeButton.OnPressed += _ => SetSort(SortColumn.Time, ref _publishedSort, ref _publishedSortAscending);
        _window.PublishedSortCompoundButton.OnPressed += _ => SetSort(SortColumn.Compound, ref _publishedSort, ref _publishedSortAscending);

        Refresh();
    }

    private void SetSort(SortColumn column, ref SortColumn current, ref bool ascending)
    {
        if (current == column)
            ascending = !ascending;
        else
        {
            current = column;
            ascending = true;
        }

        Refresh();
    }

    private static IEnumerable<(string Category, DocumentEntry Entry)> ApplySort(
        IEnumerable<(string Category, DocumentEntry Entry)> flattened,
        SortColumn column,
        bool ascending)
    {
        var ordered = column == SortColumn.Time
            ? flattened.OrderBy(x => x.Entry.Time)
            : flattened.OrderBy(x => GetCompoundName(x.Entry.Title).ToUpperInvariant());

        return ascending ? ordered : ordered.Reverse();
    }

    private static void UpdateSortButtonText(Button timeButton, Button compoundButton, string timeLabel, SortColumn column, bool ascending)
    {
        var arrow = ascending ? "▲" : "▼";
        timeButton.Text = column == SortColumn.Time ? $"{timeLabel} {arrow}" : timeLabel;
        compoundButton.Text = column == SortColumn.Compound ? $"Compound {arrow}" : "Compound";
    }

    public void Refresh()
    {
        if (_window is not { IsOpen: true })
            return;

        if (!_research.TryGetResearch(out var researchEnt))
            return;

        var research = researchEnt.Value.Comp;
        var credits = research.Credits;
        var clearance = research.ClearanceLevel;

        _window.ClearanceLabel.Text = $"CLEARANCE LEVEL {clearance}";
        _window.CreditsLabel.Text = $"Credits available: {credits}";

        _window.ReprintContractButton.Disabled = string.IsNullOrEmpty(research.LastPickedContractReagent);

        if (clearance < research.MaxClearanceLevel)
        {
            var cost = _research.GetClearanceUpgradeCost(research);
            _window.BrokerClearanceLabel.Text = $"Improve ({cost}CR)";
            _window.BrokerClearanceButton.Disabled = cost > credits;
            SetPurchasableStyle(_window.BrokerClearanceButton, cost <= credits, AmberWashed);
        }
        else if (!research.ReachedXAccess)
        {
            _window.BrokerClearanceLabel.Text = $"Request 5X Access ({research.XAccessCost}CR)";
            _window.BrokerClearanceButton.Disabled = research.XAccessCost > credits;
            SetPurchasableStyle(_window.BrokerClearanceButton, research.XAccessCost <= credits, AmberWashed);
        }
        else
        {
            _window.BrokerClearanceLabel.Text = "5X Access Granted";
            _window.BrokerClearanceButton.Disabled = true;
            SetPurchasableStyle(_window.BrokerClearanceButton, false, AmberWashed);
        }

        var duration = research.PickedThisCycle ? research.ContractRerollPicked : research.ContractRerollNotPicked;
        _window.CycleStart = research.NextReroll - duration;
        _window.CycleEnd = research.NextReroll;

        PopulateContracts(research);
        PopulateDocuments(_window.DocumentsBox, _window.DocumentsEmptyLabel, research, research.Documents, isPublishedTab: false);
        PopulateDocuments(_window.PublishedBox, _window.PublishedEmptyLabel, research, research.Published, isPublishedTab: true);
    }

    private void OnBrokerClearancePressed()
    {
        if (!_research.TryGetResearch(out var research))
            return;

        var cost = _research.GetClearanceUpgradeCost(research.Value.Comp);
        OpenConfirmation(
            "Improve Clearance",
            $"Are you sure you want to spend {cost} research credits to increase the clearance immediately?",
            new RMCResearchComputerBrokerClearanceBuiMsg());
    }

    private void OnRequestXAccessPressed()
    {
        var xAccessCost = _research.TryGetResearch(out var research) ? research.Value.Comp.XAccessCost : 0;
        OpenConfirmation(
            "Request 5X Access",
            $"Are you sure you want to spend {xAccessCost} research credits to request 5X clearance access?",
            new RMCResearchComputerRequestXAccessBuiMsg());
    }

    private void OpenConfirmation(string title, string text, BoundUserInterfaceMessage message)
    {
        _confirm?.Close();

        var confirm = new ConfirmationWindow();
        _confirm = confirm;
        confirm.Setup(title, text, "Confirm", "Cancel");
        confirm.AcceptButton.OnPressed += _ =>
        {
            SendMessage(message);
            confirm.Close();
        };
        confirm.DenyButton.OnPressed += _ => confirm.Close();
        confirm.OnClose += () =>
        {
            if (_confirm == confirm)
                _confirm = null;
        };
        confirm.OpenCentered();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        _confirm?.Close();
        _confirm = null;
    }

    private void PopulateContracts(RMCChemistryResearchComponent research)
    {
        var box = _window!.ContractsBox;

        for (var i = 0; i < research.Contracts.Count; i++)
        {
            var slot = research.Contracts[i];

            RMCResearchContractCard card;
            if (i < box.ChildCount && box.GetChild(i) is RMCResearchContractCard existing)
            {
                card = existing;
            }
            else
            {
                card = new RMCResearchContractCard();
                box.AddChild(card);
            }

            var propertyName = _prototype.TryIndex<ChemGeneratorPropertyPrototype>(slot.PropertyHintId, out var property)
                ? property.ID
                : slot.PropertyHintId;
            var ingredientName = _rmcReagent.TryIndex(slot.IngredientHintId, out var ingredientReagent)
                ? ingredientReagent.LocalizedName
                : slot.IngredientHintId;

            card.NameLabel.Text = slot.Name;
            card.DifficultyLabel.Text = "Difficulty: " + slot.Tier switch
            {
                1 => "Easy",
                2 => "Intermediate",
                _ => "Hard",
            };
            card.RecipeHintLabel.Text = $"Early assessment shows one part of the recipe is {ingredientName}";
            card.PropertyHintLabel.Text = $"Early testing shows property of {propertyName}";
            card.SetTakeState(!slot.Taken && !research.PickedThisCycle, slot.Taken ? "Contract Taken" : "Take Contract");
            card.SlotIndex = i;
            card.OnTake = index => SendMessage(new RMCResearchComputerTakeContractBuiMsg(index));
        }

        box.RemoveChildrenAfter(research.Contracts.Count);
    }

    private void PopulateDocuments(BoxContainer box, Label emptyLabel, RMCChemistryResearchComponent research, Dictionary<string, List<DocumentEntry>> documents, bool isPublishedTab)
    {
        var sortColumn = isPublishedTab ? _publishedSort : _documentsSort;
        var sortAscending = isPublishedTab ? _publishedSortAscending : _documentsSortAscending;
        var timeButton = isPublishedTab ? _window!.PublishedSortTimeButton : _window!.DocumentsSortTimeButton;
        var compoundButton = isPublishedTab ? _window!.PublishedSortCompoundButton : _window!.DocumentsSortCompoundButton;
        UpdateSortButtonText(timeButton, compoundButton, isPublishedTab ? "Published" : "Scan Time", sortColumn, sortAscending);

        var hideOld = _window!.HideOldReportsButton.Pressed;

        var publishedKeys = research.Published
            .SelectMany(kv => kv.Value.Select(e => (Category: kv.Key, Compound: GetCompoundName(e.Title))))
            .ToHashSet();

        var flattened = ApplySort(
            documents.SelectMany(kv => kv.Value.Select(entry => (Category: kv.Key, Entry: entry))),
            sortColumn,
            sortAscending).ToList();

        if (hideOld)
        {
            var seen = new HashSet<string>();
            flattened = flattened.Where(x => seen.Add(x.Entry.Title)).ToList();
        }

        emptyLabel.Visible = flattened.Count == 0;

        var index = 0;
        foreach (var (category, entry) in flattened)
        {
            RMCResearchDocumentRow row;
            if (index < box.ChildCount && box.GetChild(index) is RMCResearchDocumentRow existing)
            {
                row = existing;
            }
            else
            {
                row = new RMCResearchDocumentRow();
                box.AddChild(row);
            }

            var rowPublished = isPublishedTab || publishedKeys.Contains((category, GetCompoundName(entry.Title)));

            row.ScanTimeLabel.Text = entry.Time.ToString(@"hh\:mm\:ss");
            row.TypeLabel.Text = category;
            row.CompoundLabel.Text = GetCompoundName(entry.Title);
            row.Id = entry.Id;
            row.Published = rowPublished;
            row.PublishButton.Text = rowPublished ? "Unpublish" : "Publish";
            row.OnRead = id => SendMessage(new RMCResearchComputerReadDocumentBuiMsg(id));
            row.OnPrint = id => SendMessage(new RMCResearchComputerPrintDocumentBuiMsg(id));
            row.OnPublishToggle = (id, published) => SendMessage(published
                ? new RMCResearchComputerUnpublishDocumentBuiMsg(id)
                : new RMCResearchComputerPublishDocumentBuiMsg(id));
            row.OnAnnounce = id => SendMessage(new RMCResearchComputerAnnounceDocumentBuiMsg(id));

            index++;
        }

        box.RemoveChildrenAfter(index);
    }

    private static string GetCompoundName(string title)
    {
        var dashIndex = title.IndexOf(" - ", StringComparison.Ordinal);
        return dashIndex < 0 ? title : title[(dashIndex + 3)..];
    }
}
