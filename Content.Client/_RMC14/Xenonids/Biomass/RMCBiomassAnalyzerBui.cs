using System.Linq;
using Content.Client._RMC14.UserInterface;
using Content.Shared._RMC14.Chemistry.Generation;
using Content.Shared._RMC14.UserInterface;
using Content.Shared._RMC14.Xenonids.Biomass;
using Content.Shared.Containers.ItemSlots;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using static Content.Client._RMC14.Xenonids.Biomass.RMCBiomassAnalyzerWindow;

namespace Content.Client._RMC14.Xenonids.Biomass;

[UsedImplicitly]
public sealed class RMCBiomassAnalyzerBui : BoundUserInterface, IRefreshableBui
{
    private readonly SharedRMCBiomassAnalyzerDataSystem _analyzerData;
    private readonly ItemSlotsSystem _itemSlots;
    private readonly SharedRMCChemistryResearchSystem _research;

    private RMCBiomassAnalyzerWindow? _window;
    private RMCBiomassUpgradeCategory _selectedCategory = RMCBiomassUpgradeCategory.Machinery;

    public RMCBiomassAnalyzerBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _analyzerData = EntMan.System<SharedRMCBiomassAnalyzerDataSystem>();
        _itemSlots = EntMan.System<ItemSlotsSystem>();
        _research = EntMan.System<SharedRMCChemistryResearchSystem>();
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<RMCBiomassAnalyzerWindow>();

        BuildCategoryBox();

        _window.EjectOrganButton.OnPressed += _ => SendMessage(new RMCBiomassAnalyzerEjectOrganBuiMsg());
        _window.ProcessOrganButton.OnPressed += _ => SendMessage(new RMCBiomassAnalyzerToggleAutoProcessBuiMsg());
        _window.ToggleQueueButton.OnPressed += _ => SendMessage(new RMCBiomassAnalyzerToggleQueueBuiMsg());

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

    public void Refresh()
    {
        if (_window is not { IsOpen: true })
            return;

        if (!_analyzerData.TryGetAnalyzer(out var analyzerEnt))
            return;

        if (!EntMan.TryGetComponent(Owner, out RMCBiomassAnalyzerMachineComponent? machine))
            return;

        var analyzer = analyzerEnt.Value.Comp;
        var clearance = _research.TryGetResearch(out var research) ? research.Value.Comp.ClearanceLevel : 1;

        _window.PointsLabel.Text = $"Biological Matter: {analyzer.Points}";

        var hasOrgan = _itemSlots.GetItemOrNull(Owner, machine.OrganSlotId) != null;
        var busy = machine.PrintingUntil != null;
        _window.EjectOrganButton.Disabled = !hasOrgan || busy;
        _window.OrganStatusLabel.Text = hasOrgan
            ? "Biomass accepted. Ready to analyze."
            : "Receptacle is empty, analyzing is impossible!";
        _window.OrganStatusLabel.FontColorOverride = hasOrgan ? Amber : AmberDim;

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
            var entry = new RMCBiomassQueueEntry { Index = index };
            entry.NameLabel.Text = _analyzerData.TryGetUpgrade(upgradeId, out var upgrade)
                ? upgrade.Prototype.Name
                : upgradeId.Id;
            entry.OnCancel = i => SendMessage(new RMCBiomassAnalyzerRemoveFromQueueBuiMsg(i));

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
            RMCBiomassUpgradeRow row;
            if (index < box.ChildCount && box.GetChild(index) is RMCBiomassUpgradeRow existing)
            {
                row = existing;
            }
            else
            {
                row = new RMCBiomassUpgradeRow();
                box.AddChild(row);
            }

            var cost = _analyzerData.GetCurrentCost(upgrade, analyzer);

            row.NameLabel.Text = upgrade.Prototype.Name;
            row.ClearanceLabel.Text = $"Clearance {upgrade.Comp.RequiredClearance} Required";
            row.TrendLabel.Text = upgrade.Comp.PriceChange switch
            {
                0 => "This technology will not change its value with purchase.",
                > 0 => $"This technology will increase in cost by {upgrade.Comp.PriceChange} each purchase.",
                _ => $"This technology will decrease in cost by {-upgrade.Comp.PriceChange} each purchase.",
            };

            row.PrintButtonLabel.Text = $"Print ({cost})";
            row.SetClearanceMet(clearance >= upgrade.Comp.RequiredClearance);
            row.UpgradeId = upgrade.Id;
            row.OnPrint = (id, amount) => SendMessage(new RMCBiomassAnalyzerEnqueuePrintBuiMsg(id, amount));

            index++;
        }

        box.RemoveChildrenAfter(index);
    }
}
