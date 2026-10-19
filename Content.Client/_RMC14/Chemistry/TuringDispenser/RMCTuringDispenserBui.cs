using System.Linq;
using Content.Client._RMC14.UserInterface;
using Content.Client.Chemistry.Containers.EntitySystems;
using Content.Client.Resources;
using Content.Client.UserInterface.ControlExtensions;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared._RMC14.Chemistry.SmartFridge;
using Content.Shared._RMC14.Chemistry.TuringDispenser;
using Content.Shared._RMC14.UserInterface;
using Content.Shared._RMC14.Vendors;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Chemistry.TuringDispenser;

[UsedImplicitly]
public sealed class RMCTuringDispenserBui : BoundUserInterface, IRefreshableBui
{
    private const float VialMaxVolume = 30f;

    private const float MinMultiplier = 0.1f;
    private const float MaxMultiplier = 10f;
    private const float MinCycles = 1f;
    private const float MaxCycles = 100f;

    private static readonly Color HeaderColor = Color.FromHex("#12141A");
    private static readonly Color GreyButtonFill = Color.FromHex("#bfbfbf");
    private static readonly Color GreenColor = Color.FromHex("#4CAF50");
    private static readonly Color TanColor = Color.FromHex("#ffb950");
    private static readonly Color OrangeColor = Color.FromHex("#C99A29");
    private static readonly Color RedColor = Color.FromHex("#8B2020");
    private static readonly Color DarkRowColor = Color.FromHex("#22262F");

    private readonly SharedCMAutomatedVendorSystem _cmVendor;
    private readonly ContainerSystem _container;
    private readonly IPrototypeManager _prototype;
    private readonly IResourceCache _resourceCache;
    private readonly RMCReagentSystem _rmcReagent;
    private readonly SharedRMCSmartFridgeSystem _smartFridge;
    private readonly SolutionContainerSystem _solution;
    private readonly Font _boldItalicFont;

    private RMCTuringDispenserWindow? _window;
    private FloatSpinBox? _multiplierSpinBox;
    private FloatSpinBox? _cyclesSpinBox;
    private readonly List<RMCTuringDispenserBeakerRow> _beakerRows = new();

    private RMCIconLabelRow? _autoRunIcon;
    private Label? _autoRunLabel;
    private Label? _smartLinkLabel;
    private Label? _outputModeLabel;

    public RMCTuringDispenserBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _cmVendor = EntMan.System<SharedCMAutomatedVendorSystem>();
        _container = EntMan.System<ContainerSystem>();
        _prototype = IoCManager.Resolve<IPrototypeManager>();
        _resourceCache = IoCManager.Resolve<IResourceCache>();
        _rmcReagent = EntMan.System<RMCReagentSystem>();
        _smartFridge = EntMan.System<SharedRMCSmartFridgeSystem>();
        _solution = EntMan.System<SolutionContainerSystem>();
        _boldItalicFont = _resourceCache.GetFont("/Fonts/NotoSans/NotoSans-BoldItalic.ttf", 12);
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<RMCTuringDispenserWindow>();

        Header(_window.ControlsHeader);
        Header(_window.SettingsHeader);
        Header(_window.BeakerSelectorHeader);
        Header(_window.MemoryHeader);
        Header(_window.BoxHeader);

        var autoRow = new RMCTuringDispenserBeakerRow { Id = null };
        autoRow.Icon.Texture = _resourceCache.GetTexture(RMCIcons.ArrowsRotate);
        autoRow.Icon.ModulateSelfOverride = RMCIcons.Idle;
        autoRow.NameLabel.Text = "Auto (best fit)";
        autoRow.OnPressed += _ => SendPredictedMessage(new RMCTuringDispenserSetPreferredBeakerBuiMsg(null));
        _window.BeakerSelectorContainer.AddChild(autoRow);
        _beakerRows.Add(autoRow);

        foreach (var (id, maxVol, _) in SharedRMCTuringDispenserSystem.BeakerOptions)
        {
            var row = new RMCTuringDispenserBeakerRow { Id = id };

            var name = id.Id;
            if (_prototype.TryIndex(id, out var entityProto))
            {
                name = entityProto.Name;
                var textures = SpriteComponent.GetPrototypeTextures(entityProto, _resourceCache).Select(o => o.Default).ToList();
                if (textures.Count > 0)
                    row.Icon.Texture = textures[0];
            }

            row.NameLabel.Text = $"{name} ({maxVol}u)";
            row.OnPressed += _ => SendPredictedMessage(new RMCTuringDispenserSetPreferredBeakerBuiMsg(id));

            _window.BeakerSelectorContainer.AddChild(row);
            _beakerRows.Add(row);
        }

        _window.EnergyBar.ForegroundStyleBoxOverride = Flat(GreenColor);

        GreyButton(_window.ClearMemoryButton);
        _window.ClearMemoryButton.AddIconLabel(RMCIcons.Trash, "Clear memory");
        GreyButton(_window.RunProgramButton);
        _window.RunProgramButton.AddIconLabel(RMCIcons.Play, "Run program");
        GreyButton(_window.SaveToMemoryButton);
        _window.SaveToMemoryButton.AddIconLabel(RMCIcons.FloppyDisk, "Save box to memory");
        GreyButton(_window.EjectBoxButton);
        _window.EjectBoxButton.AddIconLabel(RMCIcons.Eject, "Eject box");
        GreyButton(_window.DisposeBeakerButton);
        _window.DisposeBeakerButton.AddIconLabel(RMCIcons.Flask, "Flush beaker");
        GreyButton(_window.EjectBeakerButton);
        _window.EjectBeakerButton.AddIconLabel(RMCIcons.Eject, "Eject beaker");

        Toggle(_window.AutoRunButton);
        _autoRunIcon = _window.AutoRunButton.AddIconLabel(RMCIcons.Stop, "Autorun: disabled");
        _autoRunLabel = _autoRunIcon.Label;
        Toggle(_window.SmartLinkButton);
        _smartLinkLabel = _window.SmartLinkButton.AddIconLabel(RMCIcons.Link, "Smartlink: enabled").Label;
        Toggle(_window.OutputModeButton);
        _outputModeLabel = _window.OutputModeButton.AddIconLabel(RMCIcons.Flask, "Outputting to: CONTAINER").Label;

        _window.SaveToMemoryButton.OnPressed += _ => SendPredictedMessage(new RMCTuringDispenserSaveToMemoryBuiMsg());
        _window.EjectBoxButton.OnPressed += _ => SendPredictedMessage(new RMCTuringDispenserEjectBoxBuiMsg());
        _window.DisposeBeakerButton.OnPressed += _ => SendPredictedMessage(new RMCTuringDispenserDisposeBeakerBuiMsg());
        _window.EjectBeakerButton.OnPressed += _ => SendPredictedMessage(new RMCTuringDispenserEjectBeakerBuiMsg());
        _window.ClearMemoryButton.OnPressed += _ => SendPredictedMessage(new RMCTuringDispenserClearMemoryBuiMsg());
        _window.RunProgramButton.OnPressed += _ => SendPredictedMessage(new RMCTuringDispenserRunProgramBuiMsg());
        _window.AutoRunButton.OnPressed += _ => SendPredictedMessage(new RMCTuringDispenserToggleAutoRunBuiMsg());
        _window.SmartLinkButton.OnPressed += _ => SendPredictedMessage(new RMCTuringDispenserToggleSmartLinkBuiMsg());
        _window.OutputModeButton.OnPressed += _ => SendPredictedMessage(new RMCTuringDispenserToggleOutputModeBuiMsg());

        var initialMultiplier = EntMan.TryGetComponent(Owner, out RMCTuringDispenserComponent? comp) ? comp.Multiplier : FixedPoint2.New(1);
        var multiplier = initialMultiplier.Float();
        _multiplierSpinBox = UIExtensions.CreateDialSpinBox(multiplier,
            args => SendPredictedMessage(new RMCTuringDispenserSetMultiplierBuiMsg(FixedPoint2.New(args.Value))));
        _multiplierSpinBox.IsValid = value => value is >= MinMultiplier and <= MaxMultiplier;
        _window.MultiplierContainer.AddChild(_multiplierSpinBox);

        var cycles = comp?.CycleLimit ?? 1;
        _cyclesSpinBox = UIExtensions.CreateDialSpinBox(cycles,
            args => SendPredictedMessage(new RMCTuringDispenserSetCyclesBuiMsg((int) args.Value)));
        _cyclesSpinBox.IsValid = value => value is >= MinCycles and <= MaxCycles;
        _window.CyclesContainer.AddChild(_cyclesSpinBox);

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
        _window.EnergyBar.MaxValue = maxEnergy.Float();
        _window.EnergyBar.Value = energy.Float();
        _window.EnergyLabel.Text = $"Energy: {(maxEnergy.Float() > 0 ? energy.Float() / maxEnergy.Float() * 100 : 0):0}%";

        var (statusText, statusColor) = comp.Status switch
        {
            TuringDispenserStatus.Idle => ("Status: IDLE", TanColor),
            TuringDispenserStatus.Running => ("Status: RUNNING", OrangeColor),
            TuringDispenserStatus.Finished => ("Status: FINISHED", GreenColor),
            TuringDispenserStatus.Stuck => ($"Status: STUCK - {comp.Error}", RedColor),
            _ => ("Status: IDLE", TanColor),
        };
        Row(_window.StatusPanel, _window.StatusLabel, statusText, statusColor, _boldItalicFont);

        UpdateBeakerRows(comp);

        UpdateProgramRows(_window.MemoryProgramContainer, _window.MemoryEmptyPanel, _window.MemoryEmptyLabel, comp.MemoryProgram);
        UpdateProgramRows(_window.BoxProgramContainer, _window.BoxEmptyPanel, _window.BoxEmptyLabel, comp.BoxProgram);

        if (_container.TryGetContainer(Owner, comp.InputBoxSlotId, out var boxContainer) &&
            boxContainer.ContainedEntities.Count > 0)
        {
            Row(_window.BoxAlertPanel, _window.BoxStatusLabel, "Box loaded", TanColor, _boldItalicFont);
        }
        else
        {
            Row(_window.BoxAlertPanel, _window.BoxStatusLabel, "No input box loaded!", RedColor, _boldItalicFont);
        }

        if (_container.TryGetContainer(Owner, comp.OutputBeakerSlotId, out var beakerContainer) &&
            beakerContainer.ContainedEntities.Count > 0 &&
            _solution.TryGetMixableSolution(beakerContainer.ContainedEntities[0], out _, out var solution))
        {
            Row(_window.BeakerAlertPanel, _window.BeakerStatusLabel, $"{solution.Volume}/{solution.MaxVolume} units", TanColor, _boldItalicFont);
        }
        else
        {
            Row(_window.BeakerAlertPanel, _window.BeakerStatusLabel, "No output beaker loaded!", RedColor, _boldItalicFont);
        }

        if (_autoRunLabel != null)
            _autoRunLabel.Text = comp.AutoRun ? "Autorun: enabled" : "Autorun: disabled";

        _autoRunIcon?.SetIcon(comp.AutoRun ? RMCIcons.ArrowsRotate : RMCIcons.Stop);

        if (_smartLinkLabel != null)
            _smartLinkLabel.Text = comp.SmartLink ? "Smartlink: enabled" : "Smartlink: disabled";

        if (_outputModeLabel != null)
        {
            _outputModeLabel.Text = comp.OutputMode switch
            {
                TuringDispenserOutputMode.Container => "Outputting to: CONTAINER",
                TuringDispenserOutputMode.SmartFridge => "Outputting to: SMARTFRIDGE",
                TuringDispenserOutputMode.Centrifuge => "Outputting to: CENTRIFUGE",
                _ => "Outputting to: CONTAINER",
            };
        }

        if (_multiplierSpinBox != null)
        {
            var multiplierValue = comp.Multiplier;
            _multiplierSpinBox.Value = multiplierValue.Float();
        }

        if (_cyclesSpinBox != null)
            _cyclesSpinBox.Value = comp.CycleLimit;
    }

    private void UpdateBeakerRows(RMCTuringDispenserComponent comp)
    {
        var coords = EntMan.GetComponent<TransformComponent>(Owner).Coordinates;

        foreach (var row in _beakerRows)
        {
            if (row.Id is { } id)
            {
                var vendorStocked = SharedRMCTuringDispenserSystem.BeakerOptions.First(o => o.Id == id).VendorStocked;

                if (!vendorStocked)
                {
                    row.StockLabel.Text = "∞";
                }
                else
                {
                    var stock = _smartFridge.GetEmptyContainerCount(coords, comp.SmartFridgeRange, id) +
                                _cmVendor.GetStockedAmount(coords, comp.SmartFridgeRange, id);
                    row.StockLabel.Text = $"x{stock}";
                }
            }
            else
            {
                row.StockLabel.Text = string.Empty;
            }

            var selected = row.Id == comp.PreferredBeaker;
            row.StyleBoxOverride = Flat(selected ? GreenColor : DarkRowColor);
            row.ModulateSelfOverride = Color.White;
        }
    }

    private void UpdateProgramRows(BoxContainer container, Control emptyPanel, Label emptyLabel, List<TuringProgramEntry> program)
    {
        emptyPanel.Visible = program.Count == 0;
        if (program.Count == 0)
            Row(emptyPanel, emptyLabel, emptyLabel.Text ?? string.Empty, RedColor, _boldItalicFont);

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
            row.SetFillFraction(entry.Amount.Float() / VialMaxVolume);
        }

        container.RemoveChildrenAfter(program.Count);
    }

    private static void Header(PanelContainer panel)
    {
        panel.PanelOverride = Flat(HeaderColor);
    }

    private static void GreyButton(Button button)
    {
        button.StyleBoxOverride = Flat(GreyButtonFill);
        button.ModulateSelfOverride = Color.White;
    }

    private static void Toggle(Button button)
    {
        button.StyleBoxOverride = Flat(TanColor);
        button.ModulateSelfOverride = Color.White;
    }

    private static void Row(Control panelControl, Label label, string text, Color color, Font font)
    {
        label.Text = text;
        label.FontColorOverride = color == RedColor ? Color.White : Color.Black;
        label.FontOverride = font;

        if (panelControl is PanelContainer panel)
            panel.PanelOverride = Flat(color);
    }

    private static StyleBoxFlat Flat(Color color)
    {
        return new StyleBoxFlat
        {
            BackgroundColor = color,
            ContentMarginLeftOverride = 6,
            ContentMarginRightOverride = 6,
            ContentMarginTopOverride = 1,
            ContentMarginBottomOverride = 1,
        };
    }
}
