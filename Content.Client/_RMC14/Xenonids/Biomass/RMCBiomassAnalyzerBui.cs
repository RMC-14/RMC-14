using System.Linq;
using Content.Client._RMC14.UserInterface;
using Content.Shared._RMC14.Chemistry.Generation;
using Content.Shared._RMC14.UserInterface;
using Content.Shared._RMC14.Xenonids.Biomass;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using static Content.Client._RMC14.Xenonids.Biomass.RMCBiomassAnalyzerWindow;

namespace Content.Client._RMC14.Xenonids.Biomass;

[UsedImplicitly]
public sealed class RMCBiomassAnalyzerBui : BoundUserInterface, IRefreshableBui
{
    [Dependency] private readonly IEntityManager _entity = default!;

    private RMCBiomassAnalyzerWindow? _window;
    private SharedRMCBiomassAnalyzerDataSystem _analyzerData = default!;
    private RMCBiomassUpgradeCategory _selectedCategory = RMCBiomassUpgradeCategory.Machinery;

    public RMCBiomassAnalyzerBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
        _analyzerData = _entity.System<SharedRMCBiomassAnalyzerDataSystem>();
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<RMCBiomassAnalyzerWindow>();

        BuildCategoryBox();

        _window.EjectOrganButton.OnPressed += _ => SendPredictedMessage(new RMCBiomassAnalyzerEjectOrganBuiMsg());
        _window.ProcessOrganButton.OnPressed += _ => SendPredictedMessage(new RMCBiomassAnalyzerToggleAutoProcessBuiMsg());
        _window.ToggleQueueButton.OnPressed += _ => SendPredictedMessage(new RMCBiomassAnalyzerToggleQueueBuiMsg());

        Refresh();
    }

    private void BuildCategoryBox()
    {
        _window!.CategoryBox.Clear();

        var categories = Enum.GetValues<RMCBiomassUpgradeCategory>();
        foreach (var category in categories)
        {
            _window.CategoryBox.AddItem(category.ToString(), (int) category);
        }

        _window.CategoryBox.SelectId((int) _selectedCategory);
        _window.CategoryBox.OnItemSelected += args =>
        {
            _window.CategoryBox.SelectId(args.Id);
            _selectedCategory = (RMCBiomassUpgradeCategory) args.Id;
            Refresh();
        };
    }

    private bool TryGetAnalyzer(out RMCBiomassAnalyzerComponent analyzer)
    {
        var query = _entity.EntityQueryEnumerator<RMCBiomassAnalyzerComponent>();
        if (query.MoveNext(out _, out var comp))
        {
            analyzer = comp;
            return true;
        }

        analyzer = default!;
        return false;
    }

    private int GetClearanceLevel()
    {
        var query = _entity.EntityQueryEnumerator<RMCChemistryResearchComponent>();
        if (query.MoveNext(out _, out var comp))
            return comp.ClearanceLevel;

        return 1;
    }

    public void Refresh()
    {
        if (_window is not { IsOpen: true })
            return;

        if (!TryGetAnalyzer(out var analyzer))
            return;

        if (!_entity.TryGetComponent(Owner, out RMCBiomassAnalyzerMachineComponent? machine))
            return;

        var clearance = GetClearanceLevel();

        _window.PointsLabel.Text = $"Biological Matter: {analyzer.Points}";

        var busy = machine.PrintingUntil != null;
        _window.EjectOrganButton.Disabled = machine.HeldOrgan == null || busy;
        _window.OrganStatusLabel.Text = machine.HeldOrgan == null
            ? "Receptacle is empty, analyzing is impossible!"
            : "Biomass accepted. Ready to analyze.";
        _window.OrganStatusLabel.FontColorOverride = machine.HeldOrgan == null ? AmberDim : Amber;

        _window.ProcessOrganIcon.Label.Text = machine.AutoProcessOrgan ? "Auto: ON" : "Auto: OFF";

        PopulateQueue(machine);
        PopulateCategory(analyzer, clearance);
    }

    private void PopulateQueue(RMCBiomassAnalyzerMachineComponent machine)
    {
        _window!.ToggleQueueIcon.SetIcon(machine.QueueProcessing ? RMCIcons.Stop : RMCIcons.Play);
        _window.ToggleQueueIcon.Label.Text = machine.QueueProcessing ? "STOP" : "START";

        _window.QueueEmptyLabel.Visible = machine.PrintQueue.Count == 0;

        _window.QueueBox.DisposeAllChildren();

        var index = 0;
        foreach (var upgradeId in machine.PrintQueue)
        {
            var entry = new QueueEntryControl { Index = index };
            entry.NameLabel.Text = _analyzerData.TryGetUpgrade(upgradeId, out var upgrade)
                ? upgrade.Prototype.Name
                : upgradeId;
            entry.OnCancel = i => SendPredictedMessage(new RMCBiomassAnalyzerRemoveFromQueueBuiMsg(i));

            _window.QueueBox.AddChild(entry);
            index++;
        }
    }

    private void PopulateCategory(RMCBiomassAnalyzerComponent analyzer, int clearance)
    {
        var box = _window!.UpgradesBox;
        var upgrades = _analyzerData.Upgrades
            .Where(u => u.Comp.Category == _selectedCategory)
            .OrderBy(u => u.Comp.RequiredClearance)
            .ThenBy(u => u.Comp.Cost)
            .ToList();

        var index = 0;
        foreach (var upgrade in upgrades)
        {
            UpgradeRow row;
            if (index < box.ChildCount && box.GetChild(index) is UpgradeRow existing)
            {
                row = existing;
            }
            else
            {
                row = new UpgradeRow();
                box.AddChild(row);
            }

            var cost = _analyzerData.GetCurrentCost(upgrade, analyzer);
            var clearanceMet = clearance >= upgrade.Comp.RequiredClearance;

            row.NameLabel.Text = upgrade.Prototype.Name;
            row.ClearanceLabel.Text = $"Clearance {upgrade.Comp.RequiredClearance} Required";
            row.TrendLabel.Text = upgrade.Comp.PriceChange switch
            {
                0 => "This technology will not change its value with purchase.",
                > 0 => $"This technology will increase in cost by {upgrade.Comp.PriceChange} each purchase.",
                _ => $"This technology will decrease in cost by {-upgrade.Comp.PriceChange} each purchase.",
            };

            row.PrintButtonLabel.Text = $"Print ({cost})";

            var tint = clearanceMet ? Color.White : new Color(1f, 1f, 1f, 0.5f);
            row.PrintButton.Disabled = false;
            row.PrintButton.Modulate = tint;
            row.Print2Button.Disabled = false;
            row.Print2Button.Modulate = tint;
            row.Print5Button.Disabled = false;
            row.Print5Button.Modulate = tint;
            row.Print10Button.Disabled = false;
            row.Print10Button.Modulate = tint;

            row.UpgradeId = upgrade.Prototype.ID;
            row.OnPrint = (id, amount) => SendPredictedMessage(new RMCBiomassAnalyzerEnqueuePrintBuiMsg(id, amount));

            index++;
        }

        box.RemoveChildrenAfter(index);
    }
}
