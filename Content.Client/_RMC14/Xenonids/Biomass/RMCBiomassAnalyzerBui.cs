using System.Linq;
using Content.Client._RMC14.UserInterface;
using Content.Shared._RMC14.Chemistry.Generation;
using Content.Shared._RMC14.UserInterface;
using Content.Shared._RMC14.Xenonids.Biomass;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using static Content.Client._RMC14.Xenonids.Biomass.RMCBiomassAnalyzerColors;

namespace Content.Client._RMC14.Xenonids.Biomass;

[UsedImplicitly]
public sealed class RMCBiomassAnalyzerBui : BoundUserInterface, IRefreshableBui
{
    private readonly SharedRMCBiomassAnalyzerDataSystem _analyzerData;
    private readonly SharedRMCBiomassAnalyzerMachineSystem _machine;
    private readonly SharedRMCChemistryResearchSystem _research;

    private RMCBiomassAnalyzerWindow? _window;
    private RMCBiomassUpgradeCategory _selectedCategory = RMCBiomassUpgradeCategory.Machinery;

    public RMCBiomassAnalyzerBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _analyzerData = EntMan.System<SharedRMCBiomassAnalyzerDataSystem>();
        _machine = EntMan.System<SharedRMCBiomassAnalyzerMachineSystem>();
        _research = EntMan.System<SharedRMCChemistryResearchSystem>();
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<RMCBiomassAnalyzerWindow>();

        BuildCategoryBox();

        _window.EjectOrganButton.OnPressed += _ => SendMessage(new RMCBiomassAnalyzerEjectOrganBuiMsg());
        _window.ProcessOrganButton.OnPressed += _ => SendMessage(new RMCBiomassAnalyzerProcessOrganBuiMsg());
        _window.AutoProcessButton.OnPressed += _ => SendMessage(new RMCBiomassAnalyzerToggleAutoProcessBuiMsg());
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
        _window.PointsLabel.Text = $"Biological Matter: {analyzer.Points}";

        PopulateOrgan((Owner, machine));
        PopulateQueue(machine);
        PopulateCategory(analyzer);
    }

    private void PopulateOrgan(Entity<RMCBiomassAnalyzerMachineComponent> ent)
    {
        var machine = ent.Comp;
        var organ = _machine.GetFirstQueuedOrgan(ent);
        EntMan.TryGetComponent(organ, out RMCBiomassOrganComponent? organComp);
        var organCount = machine.Queue.Count(job => job.Organ != null);
        var busy = machine.PrintingUntil != null;

        _window!.EjectOrganIcon.Label.Text = organComp?.Caste is { } caste
            ? $"Eject {caste} biomass"
            : "Eject Biomass";
        _window.ProcessOrganIcon.Label.Text = organComp != null
            ? $"Process Biomass, Expected value : {organComp.Value}"
            : "Process Biomass";
        _window.AutoProcessIcon.Label.Text = machine.AutoProcessOrgan ? "Auto: ON" : "Auto: OFF";

        RMCBiomassAnalyzerWindow.SetButtonEnabled(_window.EjectOrganButton, organ != null);
        RMCBiomassAnalyzerWindow.SetButtonEnabled(_window.ProcessOrganButton, organComp != null && !busy);

        if (busy)
        {
            _window.OrganStatusLabel.Text = machine.ProcessingValue > 0
                ? $"Processing biomass, extracting {machine.ProcessingValue} biomass points..."
                : "Printing upgrade...";
            _window.OrganStatusLabel.FontColorOverride = Amber;
        }
        else if (organCount > 0)
        {
            _window.OrganStatusLabel.Text = organCount == 1
                ? "1 biomass specimen queued. Ready to analyze."
                : $"{organCount} biomass specimens queued. Ready to analyze.";
            _window.OrganStatusLabel.FontColorOverride = Amber;
        }
        else
        {
            _window.OrganStatusLabel.Text = "Receptacle is empty, analyzing is impossible!";
            _window.OrganStatusLabel.FontColorOverride = AmberDim;
        }
    }

    private void PopulateQueue(RMCBiomassAnalyzerMachineComponent machine)
    {
        _window!.ToggleQueueIcon.SetIcon(machine.QueueProcessing ? RMCIcons.Stop : RMCIcons.Play);
        _window.ToggleQueueIcon.Label.Text = machine.QueueProcessing ? "STOP" : "START";

        _window.QueueEmptyLabel.Visible = machine.Queue.Count == 0;

        var box = _window.QueueBox;
        var index = 0;
        foreach (var job in machine.Queue)
        {
            RMCBiomassQueueEntry entry;
            if (index < box.ChildCount && box.GetChild(index) is RMCBiomassQueueEntry existing)
            {
                entry = existing;
            }
            else
            {
                entry = new RMCBiomassQueueEntry();
                entry.OnCancel = i => SendMessage(new RMCBiomassAnalyzerRemoveFromQueueBuiMsg(i));
                box.AddChild(entry);
            }

            entry.Index = index;
            if (job.Organ is { } netOrgan)
            {
                EntMan.TryGetComponent(EntMan.GetEntity(netOrgan), out RMCBiomassOrganComponent? organ);
                var caste = organ?.Caste is { } c ? $"{c} " : string.Empty;
                entry.NameLabel.Text = $"Analyze {caste}biomass ({organ?.Value ?? 0})";
                entry.ToolTip = "Click to eject this specimen.";
            }
            else
            {
                entry.NameLabel.Text = job.Upgrade is { } upgradeId && _analyzerData.TryGetUpgrade(upgradeId, out var upgrade)
                    ? upgrade.Prototype.Name
                    : job.Upgrade?.Id ?? string.Empty;
                entry.ToolTip = "Click to cancel this print job.";
            }

            index++;
        }

        box.RemoveChildrenAfter(index);
    }

    private void PopulateCategory(RMCBiomassAnalyzerComponent analyzer)
    {
        var box = _window!.UpgradesBox;
        var upgrades = _analyzerData.Upgrades
            .Where(u => u.Comp.Category == _selectedCategory)
            .OrderBy(u => u.Comp.RequiredClearance)
            .ThenBy(u => u.Comp.Cost)
            .ToList();

        _research.TryGetResearch(out var research);

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
                row.OnPrint = (id, amount) => SendMessage(new RMCBiomassAnalyzerEnqueuePrintBuiMsg(id, amount));
                box.AddChild(row);
            }

            var cost = _analyzerData.GetCurrentCost(upgrade, analyzer);
            var tier = upgrade.Comp.RequiredClearance;
            var tierName = research is { } r && tier >= r.Comp.XAccessClearanceTier
                ? $"{r.Comp.MaxClearanceLevel}X"
                : tier.ToString();

            row.NameLabel.Text = upgrade.Prototype.Name;
            row.NameLabel.ToolTip = upgrade.Prototype.Name;
            row.SetDescription(upgrade.Prototype.Description);
            row.ClearanceLabel.Text = $"Clearance {tierName} Required";
            row.TrendLabel.Text = upgrade.Comp.PriceChange switch
            {
                0 => "This technology will not change its value with purchase.",
                > 0 => $"This technology will increase in cost by {upgrade.Comp.PriceChange} each purchase.",
                _ => $"This technology will decrease in cost by {-upgrade.Comp.PriceChange} each purchase.",
            };

            row.PrintButtonLabel.Text = $"Print ({cost})";
            row.SetClearanceMet(_research.HasSufficientClearance(tier));
            row.UpgradeId = upgrade.Id;

            index++;
        }

        box.RemoveChildrenAfter(index);
    }
}
