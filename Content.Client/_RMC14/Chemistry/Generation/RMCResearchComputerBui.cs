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
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;

    private RMCResearchComputerWindow? _window;
    private Font _boldFont = default!;
    private Font _smallFont = default!;
    private RMCReagentSystem _rmcReagent = default!;
    private SharedRMCChemistryResearchSystem _research = default!;

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
        _boldFont = _resourceCache.GetFont("/Fonts/NotoSans/NotoSans-Bold.ttf", 12);
        _smallFont = _resourceCache.GetFont("/Fonts/NotoSans/NotoSans-Regular.ttf", 8);
        _rmcReagent = _entity.System<RMCReagentSystem>();
        _research = _entity.System<SharedRMCChemistryResearchSystem>();
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<RMCResearchComputerWindow>();

        _window.BrokerClearanceButton.OnPressed += _ =>
        {
            if (TryGetResearch(out var research) && research.ClearanceLevel >= research.MaxClearanceLevel)
                OnRequestXAccessPressed();
            else
                OnBrokerClearancePressed();
        };
        _window.ReprintContractButton.OnPressed += _ => SendPredictedMessage(new RMCResearchComputerReprintContractBuiMsg());
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

        if (!TryGetResearch(out var research))
            return;

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
        if (!TryGetResearch(out var research))
            return;

        var cost = _research.GetClearanceUpgradeCost(research);

        var confirm = new ConfirmationWindow();
        confirm.Setup(
            "Improve Clearance",
            $"Are you sure you want to spend {cost} research credits to increase the clearance immediately?",
            "Confirm",
            "Cancel"
        );
        confirm.AcceptButton.OnPressed += _ =>
        {
            SendPredictedMessage(new RMCResearchComputerBrokerClearanceBuiMsg());
            confirm.Close();
        };
        confirm.DenyButton.OnPressed += _ => confirm.Close();
        confirm.OpenCentered();
    }

    private void OnRequestXAccessPressed()
    {
        var xAccessCost = TryGetResearch(out var research) ? research.XAccessCost : 0;

        var confirm = new ConfirmationWindow();
        confirm.Setup(
            "Request 5X Access",
            $"Are you sure you want to spend {xAccessCost} research credits to request 5X clearance access?",
            "Confirm",
            "Cancel"
        );
        confirm.AcceptButton.OnPressed += _ =>
        {
            SendPredictedMessage(new RMCResearchComputerRequestXAccessBuiMsg());
            confirm.Close();
        };
        confirm.DenyButton.OnPressed += _ => confirm.Close();
        confirm.OpenCentered();
    }

    private bool TryGetResearch(out RMCChemistryResearchComponent research)
    {
        var query = _entity.EntityQueryEnumerator<RMCChemistryResearchComponent>();
        if (query.MoveNext(out _, out var comp))
        {
            research = comp;
            return true;
        }

        research = default!;
        return false;
    }

    private void PopulateContracts(RMCChemistryResearchComponent research)
    {
        var box = _window!.ContractsBox;

        for (var i = 0; i < research.Contracts.Count; i++)
        {
            var slot = research.Contracts[i];

            ContractCard card;
            if (i < box.ChildCount && box.GetChild(i) is ContractCard existing)
            {
                card = existing;
            }
            else
            {
                card = new ContractCard(
                    _resourceCache.GetTexture(RMCIcons.Print),
                    _boldFont,
                    _smallFont);
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
            card.TakeButton.Disabled = slot.Taken || research.PickedThisCycle;
            SetPurchasableStyle(card.TakeButton, !card.TakeButton.Disabled);
            card.SetEnabledVisual(!card.TakeButton.Disabled);
            card.TakeButtonLabel.Text = slot.Taken ? "Contract Taken" : "Take Contract";
            card.SlotIndex = i;
            card.OnTake = index => SendPredictedMessage(new RMCResearchComputerTakeContractBuiMsg(index));
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
            DocumentRow row;
            if (index < box.ChildCount && box.GetChild(index) is DocumentRow existing)
            {
                row = existing;
            }
            else
            {
                row = new DocumentRow(
                    _resourceCache.GetTexture(RMCIcons.Book),
                    _resourceCache.GetTexture(RMCIcons.Print),
                    _resourceCache.GetTexture(RMCIcons.Radiation));
                box.AddChild(row);
            }

            var rowPublished = isPublishedTab || publishedKeys.Contains((category, GetCompoundName(entry.Title)));

            row.ScanTimeLabel.Text = entry.Time.ToString(@"hh\:mm\:ss");
            row.TypeLabel.Text = category;
            row.CompoundLabel.Text = GetCompoundName(entry.Title);
            row.Category = category;
            row.Title = entry.Title;
            row.Published = rowPublished;
            row.PublishButton.Text = rowPublished ? "Unpublish" : "Publish";
            row.OnRead = (category2, title2, published2) => SendPredictedMessage(new RMCResearchComputerReadDocumentBuiMsg(category2, title2, published2));
            row.OnPrint = (category2, title2, published2) => SendPredictedMessage(new RMCResearchComputerPrintDocumentBuiMsg(category2, title2, published2));
            row.OnPublishToggle = (category2, title2, published2) => SendPredictedMessage(published2
                ? new RMCResearchComputerUnpublishDocumentBuiMsg(category2, title2)
                : new RMCResearchComputerPublishDocumentBuiMsg(category2, title2));
            row.OnAnnounce = (category2, title2) => SendPredictedMessage(new RMCResearchComputerAnnounceDocumentBuiMsg(category2, title2));

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
