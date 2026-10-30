using System.Linq;
using Content.Client._RMC14.UserInterface;
using Content.Client.Chemistry.Containers.EntitySystems;
using Content.Client.Resources;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared._RMC14.Chemistry.SmartFridge;
using Content.Shared._RMC14.Chemistry.TuringDispenser;
using Content.Shared._RMC14.UserInterface;
using Content.Shared._RMC14.Vendors;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using static Content.Client._RMC14.UserInterface.RMCMachineUiStyle;

namespace Content.Client._RMC14.Chemistry.TuringDispenser;

[UsedImplicitly]
public sealed class RMCTuringDispenserBui : BoundUserInterface, IRefreshableBui
{
    private readonly SharedCMAutomatedVendorSystem _cmVendor;
    private readonly ItemSlotsSystem _itemSlots;
    private readonly IPrototypeManager _prototype;
    private readonly IResourceCache _resourceCache;
    private readonly RMCReagentSystem _rmcReagent;
    private readonly SharedRMCSmartFridgeSystem _smartFridge;
    private readonly SolutionContainerSystem _solution;
    private readonly RMCTuringDispenserSystem _turing;

    private RMCTuringDispenserWindow? _window;
    private readonly List<RMCTuringDispenserBeakerRow> _beakerRows = new();

    public RMCTuringDispenserBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _cmVendor = EntMan.System<SharedCMAutomatedVendorSystem>();
        _itemSlots = EntMan.System<ItemSlotsSystem>();
        _prototype = IoCManager.Resolve<IPrototypeManager>();
        _resourceCache = IoCManager.Resolve<IResourceCache>();
        _rmcReagent = EntMan.System<RMCReagentSystem>();
        _smartFridge = EntMan.System<SharedRMCSmartFridgeSystem>();
        _solution = EntMan.System<SolutionContainerSystem>();
        _turing = EntMan.System<RMCTuringDispenserSystem>();
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<RMCTuringDispenserWindow>();

        if (!EntMan.TryGetComponent(Owner, out RMCTuringDispenserComponent? comp))
            return;

        var autoRow = new RMCTuringDispenserBeakerRow { Id = null };
        autoRow.Icon.Texture = _resourceCache.GetTexture(RMCIcons.ArrowsRotate);
        autoRow.Icon.ModulateSelfOverride = ButtonTextColor;
        autoRow.NameLabel.Text = "Auto (best fit)";
        autoRow.OnPressed += _ => SendPredictedMessage(new RMCTuringDispenserSetPreferredBeakerBuiMsg(null));
        _window.BeakerSelectorContainer.AddChild(autoRow);
        _beakerRows.Add(autoRow);

        foreach (var option in comp.BeakerOptions)
        {
            var id = option.Id;
            var row = new RMCTuringDispenserBeakerRow { Id = id };

            var name = id.Id;
            if (_prototype.TryIndex(id, out var entityProto))
            {
                name = entityProto.Name;
                var textures = SpriteComponent.GetPrototypeTextures(entityProto, _resourceCache).Select(o => o.Default).ToList();
                if (textures.Count > 0)
                    row.Icon.Texture = textures[0];
            }

            row.NameLabel.Text = $"{name} ({option.MaxVolume}u)";
            row.OnPressed += _ => SendPredictedMessage(new RMCTuringDispenserSetPreferredBeakerBuiMsg(id));

            _window.BeakerSelectorContainer.AddChild(row);
            _beakerRows.Add(row);
        }

        _window.SaveToMemoryButton.OnPressed += _ => SendPredictedMessage(new RMCTuringDispenserSaveToMemoryBuiMsg());
        _window.EjectBoxButton.OnPressed += _ => SendPredictedMessage(new RMCTuringDispenserEjectBoxBuiMsg());
        _window.DisposeBeakerButton.OnPressed += _ => SendPredictedMessage(new RMCTuringDispenserDisposeBeakerBuiMsg());
        _window.EjectBeakerButton.OnPressed += _ => SendPredictedMessage(new RMCTuringDispenserEjectBeakerBuiMsg());
        _window.ClearMemoryButton.OnPressed += _ => SendPredictedMessage(new RMCTuringDispenserClearMemoryBuiMsg());
        _window.RunProgramButton.OnPressed += _ => SendPredictedMessage(new RMCTuringDispenserRunProgramBuiMsg());
        _window.AutoRunButton.OnPressed += _ => SendPredictedMessage(new RMCTuringDispenserToggleAutoRunBuiMsg());
        _window.SmartLinkButton.OnPressed += _ => SendPredictedMessage(new RMCTuringDispenserToggleSmartLinkBuiMsg());
        _window.OutputModeButton.OnPressed += _ => SendPredictedMessage(new RMCTuringDispenserToggleOutputModeBuiMsg());

        var minMultiplier = comp.MinMultiplier.Float();
        var maxMultiplier = comp.MaxMultiplier.Float();
        _window.MultiplierSpinBox.IsValid = value => value >= minMultiplier && value <= maxMultiplier;
        _window.MultiplierSpinBox.OnValueChanged += value =>
            SendPredictedMessage(new RMCTuringDispenserSetMultiplierBuiMsg(FixedPoint2.New(value)));

        var minCycles = comp.MinCycles;
        var maxCycles = comp.MaxCycles;
        _window.CyclesSpinBox.IsValid = value => value >= minCycles && value <= maxCycles;
        _window.CyclesSpinBox.OnValueChanged += value =>
            SendPredictedMessage(new RMCTuringDispenserSetCyclesBuiMsg((int) value));

        Refresh();
    }

    public void Refresh()
    {
        if (_window is not { IsOpen: true })
            return;

        if (!EntMan.TryGetComponent(Owner, out RMCTuringDispenserComponent? comp))
            return;

        var maxEnergy = comp.MaxEnergy;
        var energy = comp.Energy;
        var energyRatio = maxEnergy > 0 ? energy.Float() / maxEnergy.Float() : 0f;
        var energyColor = energyRatio < 0.1f
            ? BadColor
            : energyRatio < 0.33f ? AverageColor : GoodColor;
        Bar(_window.EnergyBar, energyRatio, energyColor);
        _window.EnergyLabel.Text = $"Energy: {energyRatio * 100:0}%";

        var (statusText, statusColor) = comp.Status switch
        {
            TuringDispenserStatus.Idle => ("Status: IDLE", NoticeColor),
            TuringDispenserStatus.Running => ("Status: RUNNING", NoticeColor),
            TuringDispenserStatus.Finished => ("Status: FINISHED", NoticeColor),
            TuringDispenserStatus.Stuck => ($"Status: STUCK - {comp.Error}", NoticeDangerColor),
            _ => ("Status: IDLE", NoticeColor),
        };
        _window.SetBanner(_window.StatusPanel, _window.StatusLabel, statusText, statusColor);

        UpdateBeakerRows((Owner, comp));

        UpdateProgramRows(comp, _window.MemoryProgramContainer, _window.MemoryEmptyPanel, _window.MemoryEmptyLabel, comp.MemoryProgram);
        UpdateProgramRows(comp, _window.BoxProgramContainer, _window.BoxEmptyPanel, _window.BoxEmptyLabel, comp.BoxProgram);

        if (_itemSlots.GetItemOrNull(Owner, comp.InputBoxSlotId) != null)
            _window.SetBanner(_window.BoxAlertPanel, _window.BoxStatusLabel, "Box loaded", NoticeColor);
        else
            _window.SetBanner(_window.BoxAlertPanel, _window.BoxStatusLabel, "No input box loaded!", NoticeDangerColor);

        if (_itemSlots.GetItemOrNull(Owner, comp.OutputBeakerSlotId) is { } beaker &&
            _solution.TryGetMixableSolution(beaker, out _, out var solution))
        {
            _window.SetBanner(_window.BeakerAlertPanel, _window.BeakerStatusLabel, $"{solution.Volume}/{solution.MaxVolume} units", NoticeColor);
        }
        else
        {
            _window.SetBanner(_window.BeakerAlertPanel, _window.BeakerStatusLabel, "No output beaker loaded!", NoticeDangerColor);
        }

        _window.SetAutoRun(comp.AutoRun);
        _window.SetSmartLink(comp.SmartLink);
        _window.SetOutputMode(comp.OutputMode switch
        {
            TuringDispenserOutputMode.SmartFridge => "Outputting to: SMARTFRIDGE",
            TuringDispenserOutputMode.Centrifuge => "Outputting to: CENTRIFUGE",
            _ => "Outputting to: CONTAINER",
        });

        _window.SetSpinBoxValues(comp.Multiplier.Float(), comp.CycleLimit);
    }

    private void UpdateBeakerRows(Entity<RMCTuringDispenserComponent> ent)
    {
        var comp = ent.Comp;
        var coords = EntMan.GetComponent<TransformComponent>(Owner).Coordinates;
        var hasFridge = _turing.TryGetSmartFridge(ent, out var fridge);

        foreach (var row in _beakerRows)
        {
            if (row.Id is { } id)
            {
                var vendorStocked = comp.BeakerOptions.FirstOrDefault(o => o.Id == id).VendorStocked;

                if (!vendorStocked)
                {
                    row.StockLabel.Text = "∞";
                }
                else
                {
                    var stock = (hasFridge ? _smartFridge.GetEmptyContainerCount(fridge, id) : 0) +
                                _cmVendor.GetStockedAmount(coords, comp.SmartFridgeRange, id);
                    row.StockLabel.Text = $"x{stock}";
                }
            }
            else
            {
                row.StockLabel.Text = string.Empty;
            }

            row.SetSelected(row.Id == comp.PreferredBeaker);
        }
    }

    private void UpdateProgramRows(
        RMCTuringDispenserComponent comp,
        BoxContainer container,
        PanelContainer emptyPanel,
        Label emptyLabel,
        List<TuringProgramEntry> program)
    {
        emptyPanel.Visible = program.Count == 0;
        if (program.Count == 0)
            _window!.SetBanner(emptyPanel, emptyLabel, emptyLabel.Text ?? string.Empty, NoticeDangerColor);

        var vialVolume = comp.BeakerOptions.FirstOrDefault(o => o.Id == comp.VialPrototype).MaxVolume.Float();

        for (var i = 0; i < program.Count; i++)
        {
            var entry = program[i];
            RMCTuringDispenserProgramRow row;
            if (i < container.ChildCount)
            {
                row = (RMCTuringDispenserProgramRow) container.GetChild(i);
            }
            else
            {
                row = new RMCTuringDispenserProgramRow();
                container.AddChild(row);
            }

            var name = entry.Reagent.Id;
            var fillColor = Color.White;
            if (_rmcReagent.TryIndex(entry.Reagent, out var reagent))
            {
                name = reagent.LocalizedName;
                fillColor = reagent.SubstanceColor;
            }

            row.ReagentLabel.Text = name;
            row.AmountLabel.Text = $"{entry.Amount}u";
            row.VialFillIcon.ModulateSelfOverride = fillColor;
            row.SetFillFraction(vialVolume > 0 ? entry.Amount.Float() / vialVolume : 1f);
        }

        container.RemoveChildrenAfter(program.Count);
    }
}
