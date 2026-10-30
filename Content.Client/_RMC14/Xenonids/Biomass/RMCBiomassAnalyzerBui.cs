using System.Linq;
using Content.Client._RMC14.UserInterface;
using Content.Shared._RMC14.Chemistry.Generation;
using Content.Shared._RMC14.UserInterface;
using Content.Shared._RMC14.Xenonids.Biomass;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

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
        _window.SetPoints(analyzer.Points);

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
            ? $"Process (+{organComp.Value})"
            : "Process Biomass";
        _window.ProcessOrganButton.ToolTip = organComp != null
            ? $"Process Biomass, Expected value : {organComp.Value}"
            : null;
        _window.AutoProcessIcon.Label.Text = machine.AutoProcessOrgan ? "Auto: ON" : "Auto: OFF";

        _window.EjectOrganButton.Disabled = organ == null;
        _window.ProcessOrganButton.Disabled = organComp == null || busy;

        if (busy)
        {
            _window.OrganStatusLabel.Text = machine.ProcessingValue > 0
                ? $"Processing biomass, extracting {machine.ProcessingValue} biomass points..."
                : "Printing upgrade...";
            _window.OrganStatusLabel.FontColorOverride = Color.White;
        }
        else if (organCount > 0)
        {
            _window.OrganStatusLabel.Text = organCount == 1
                ? "1 biomass specimen queued. Ready to analyze."
                : $"{organCount} biomass specimens queued. Ready to analyze.";
            _window.OrganStatusLabel.FontColorOverride = Color.White;
        }
        else
        {
            _window.OrganStatusLabel.Text = "Receptacle is empty, analyzing is impossible!";
            _window.OrganStatusLabel.FontColorOverride = RMCMachineUiStyle.LabelColor;
        }
    }

    private void PopulateQueue(RMCBiomassAnalyzerMachineComponent machine)
    {
        _window!.ToggleQueueIcon.SetIcon(machine.QueueProcessing ? RMCIcons.Stop : RMCIcons.Play);
        _window.ToggleQueueIcon.Label.Text = machine.QueueProcessing ? "STOP" : "START";

        var empty = machine.Queue.Count == 0;
        _window.QueueTitleLabel.Text = empty ? "Process Queue (empty)" : "Process Queue";
        _window.QueueDivider.Visible = !empty;
        _window.QueueContent.Visible = !empty;

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
        int? currentTier = null;
        var tierIndex = 0;
        foreach (var upgrade in upgrades)
        {
            var tier = upgrade.Comp.RequiredClearance;
            var tierName = research is { } r && tier >= r.Comp.XAccessClearanceTier
                ? $"{r.Comp.MaxClearanceLevel}X"
                : tier.ToString();
            var met = _research.HasSufficientClearance(tier);

            if (tier != currentTier)
            {
                currentTier = tier;
                tierIndex = 0;

                var header = GetOrAdd(box, index++, CreateTierHeader);
                header.Text = $"Clearance {tierName}";
                header.FontColorOverride = met ? RMCMachineUiStyle.ButtonSelectedColor : RMCMachineUiStyle.LabelColor;
            }

            var row = GetOrAdd(box, index++, () => new RMCBiomassUpgradeRow
            {
                OnPrint = (id, amount) => SendMessage(new RMCBiomassAnalyzerEnqueuePrintBuiMsg(id, amount)),
            });

            var cost = _analyzerData.GetCurrentCost(upgrade, analyzer);

            row.UpgradeId = upgrade.Id;
            row.NameLabel.Text = upgrade.Prototype.Name;
            row.NameLabel.ToolTip = upgrade.Prototype.Name;
            row.SetItem(upgrade.Comp.PrintedItem);
            row.SetAlternate(tierIndex++ % 2 == 1);
            row.SetDescription(upgrade.Prototype.Description);
            row.SetCost(cost, analyzer.Points >= cost);
            row.SetTrend(upgrade.Comp.PriceChange);
            row.SetClearance(tierName, met);
        }

        box.RemoveChildrenAfter(index);
    }

    private static Label CreateTierHeader()
    {
        return new Label
        {
            StyleClasses = { "LabelKeyText" },
            Margin = new Thickness(4, 3, 4, 1),
        };
    }

    private static T GetOrAdd<T>(BoxContainer box, int index, Func<T> create) where T : Control
    {
        if (index < box.ChildCount)
        {
            if (box.GetChild(index) is T existing)
                return existing;

            box.RemoveChild(index);
        }

        var control = create();
        box.AddChild(control);
        control.SetPositionInParent(index);
        return control;
    }
}
