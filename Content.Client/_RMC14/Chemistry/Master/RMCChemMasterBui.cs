using System.Linq;
using Content.Client._RMC14.UserInterface;
using Content.Client.Chemistry.Containers.EntitySystems;
using Content.Shared._RMC14.Chemistry.ChemMaster;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared._RMC14.Extensions;
using Content.Shared._RMC14.IconLabel;
using Content.Shared._RMC14.UserInterface;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.FixedPoint;
using Content.Shared.Storage;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Chemistry.Master;

[UsedImplicitly]
public sealed class RMCChemMasterBui : BoundUserInterface, IRefreshableBui
{
    [Dependency] private readonly ILocalizationManager _localization = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly ContainerSystem _container;
    private readonly ItemSlotsSystem _itemSlots;
    private readonly SolutionContainerSystem _solution;
    private readonly SpriteSystem _sprite;
    private readonly RMCChemMasterPresetManager _presetManager;

    private RMCChemMasterWindow? _window;
    private RMCChemMasterPopupWindow? _colorPopup;
    private RMCChemMasterPresetWindow? _presetWindow;
    private RMCChemMasterPresetEditWindow? _presetEditWindow;
    private RMCChemMasterPopupWindow? _presetColorPopup;
    private RMCChemMasterPopupWindow? _presetPillTypePopup;
    private FixedPoint2? _lastBottleAmount;

    private RMCPillBottleColors _selectedPresetBottleColor = RMCPillBottleColors.Orange;
    private uint _selectedPresetPillType = 1;
    private bool _hasPresetBottleColor = true;
    private bool _hasPresetPillType = true;
    private string? _editingPresetName;

    private readonly List<EntityUid> _lastPillBottleRows = new();
    private readonly Dictionary<EntityUid, RMCChemMasterPillBottleRow> _bottleRows = new();
    private readonly Dictionary<int, RMCChemMasterReagentRow> _beakerRows = new();
    private readonly Dictionary<int, RMCChemMasterReagentRow> _bufferRows = new();
    private readonly Dictionary<string, RMCChemMasterPresetRow> _presetRows = new();
    private readonly List<int> _toRemove = new();
    private bool _showQuickAccess = true;

    public RMCChemMasterBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _container = EntMan.System<ContainerSystem>();
        _itemSlots = EntMan.System<ItemSlotsSystem>();
        _solution = EntMan.System<SolutionContainerSystem>();
        _sprite = EntMan.System<SpriteSystem>();
        _presetManager = new RMCChemMasterPresetManager();
        _presetManager.Initialize();
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<RMCChemMasterWindow>();
        if (EntMan.TryGetComponent(Owner, out MetaDataComponent? metaData))
            _window.Title = metaData.EntityName;

        if (!EntMan.TryGetComponent(Owner, out RMCChemMasterComponent? chemMaster))
            return;

        _window.BeakerEjectButton.OnPressed += _ => SendPredictedMessage(new RMCChemMasterBeakerEjectMsg());
        _window.BeakerAllButton.OnPressed += _ => SendPredictedMessage(new RMCChemMasterBeakerTransferAllMsg());
        _window.BufferModeButton.OnPressed += _ =>
            SendPredictedMessage(new RMCChemMasterBufferModeMsg(chemMaster.BufferTransferMode.NextWrap()));
        _window.BufferAllButton.OnPressed += _ => SendPredictedMessage(new RMCChemMasterBufferTransferAllMsg());

        var pillAmount = UIExtensions.CreateDialSpinBox(buttons: false, minWidth: 10);
        pillAmount.OnValueChanged += args => SendPredictedMessage(new RMCChemMasterSetPillAmountMsg((int) args.Value));
        _window.PillAmountContainer.AddChild(pillAmount);

        var pillGroup = new ButtonGroup();
        for (var i = 0; i < chemMaster.PillTypes; i++)
        {
            var button = new RMCChemMasterPillButton();
            button.Button.Group = pillGroup;

            var type = i + 1;
            var specifier = new SpriteSpecifier.Rsi(chemMaster.PillRsi.RsiPath, $"pill{type}");
            button.Texture.Texture = _sprite.Frame0(specifier);

            button.Button.OnPressed += _ => SendPredictedMessage(new RMCChemMasterSetPillTypeMsg((uint) type));
            _window.PillTypeContainer.AddChild(button);
        }

        _window.CreatePillsButton.OnPressed += _ => SendPredictedMessage(new RMCChemMasterCreatePillsMsg());
        _window.PresetsButton.OnPressed += _ => OpenPresetWindow(chemMaster);
        _window.AutoSelectButton.OnPressed += _ => SendPredictedMessage(new RMCChemMasterAutoSelectToggleMsg());
        _window.SelectAllButton.OnPressed += _ =>
        {
            if (!_container.TryGetContainer(Owner, chemMaster.PillBottleContainer, out var container))
                return;

            var allSelected = chemMaster.SelectedBottles.Count == container.ContainedEntities.Count;
            SendPredictedMessage(new RMCChemMasterPillBottleSelectAllMsg(!allSelected));
        };

        UpdateQuickAccessBar();
        Refresh();
    }

    public void Refresh()
    {
        if (_window == null)
            return;

        if (!EntMan.TryGetComponent(Owner, out RMCChemMasterComponent? chemMaster))
            return;

        if (_lastBottleAmount != chemMaster.BottleSize)
        {
            _lastBottleAmount = chemMaster.BottleSize;
            _window.CreateBottleButton.Text = Loc.GetString("rmc-chem-master-create-bottle", ("amount", chemMaster.BottleSize));
        }

        UpdateBeaker((Owner, chemMaster));
        UpdatePillBottles((Owner, chemMaster));
        UpdateBuffer((Owner, chemMaster));

        var type = (int) chemMaster.SelectedType - 1;
        if (chemMaster.SelectedType < _window.PillTypeContainer.ChildCount &&
            _window.PillTypeContainer.GetChild(type) is RMCChemMasterPillButton pillButton)
        {
            pillButton.Button.Pressed = true;
        }

        foreach (var control in _window.PillAmountContainer.Children)
        {
            if (control is FloatSpinBox box)
                box.Value = chemMaster.PillAmount;
        }

        var anySelected = chemMaster.SelectedBottles.Count > 0;
        _window.CreatePillsButton.Disabled = !anySelected;
        _window.AutoSelectButton.Pressed = chemMaster.AutoSelectPillBottles;
    }

    private void UpdateBeaker(Entity<RMCChemMasterComponent> chemMaster)
    {
        if (_window == null)
            return;

        if (!_itemSlots.TryGetSlot(Owner, chemMaster.Comp.BeakerSlot, out var slot) ||
            slot.ContainerSlot?.ContainedEntity is not { } beaker ||
            !EntMan.TryGetComponent(beaker, out FitsInDispenserComponent? fits) ||
            !_solution.TryGetSolution(beaker, fits.Solution, out var solution))
        {
            _window.BeakerLabel.Text = Loc.GetString("rmc-chem-master-beaker-none");
            _window.BeakerEmptyLabel.Visible = true;
            _window.BeakerAllButton.Visible = false;
            _window.BeakerContentsContainer.RemoveAllChildren();
            _beakerRows.Clear();
            return;
        }

        _window.BeakerLabel.Text = Loc.GetString("rmc-chem-master-beaker-amount",
            ("amount", solution.Value.Comp.Solution.Volume));
        var any = solution.Value.Comp.Solution.Volume > FixedPoint2.Zero;
        _window.BeakerEmptyLabel.Visible = !any;
        _window.BeakerAllButton.Visible = any;

        for (var i = 0; i < solution.Value.Comp.Solution.Contents.Count; i++)
        {
            var content = solution.Value.Comp.Solution.Contents[i];
            if (!_beakerRows.TryGetValue(i, out var row))
            {
                row = CreateReagentRow(
                    chemMaster,
                    content,
                    setting => SendPredictedMessage(new RMCChemMasterBeakerTransferMsg(content.Reagent.Prototype, setting))
                );

                _beakerRows[i] = row;
                _window.BeakerContentsContainer.AddChild(row);
            }

            UpdateReagentRow(
                row,
                content,
                setting => SendPredictedMessage(new RMCChemMasterBeakerTransferMsg(content.Reagent.Prototype, setting))
            );

            row.AllButton.ClearOnPressed();
            row.AllButton.OnPressed += _ => SendPredictedMessage(new RMCChemMasterBeakerTransferMsg(content.Reagent.Prototype, content.Quantity));

            row.OnSubmit = null;
            row.OnSubmit += args =>
            {
                if (!double.TryParse(args.Text, out var amount))
                    return;

                SendPredictedMessage(
                    new RMCChemMasterBeakerTransferMsg(content.Reagent.Prototype, FixedPoint2.New(amount)));
            };
        }

        _toRemove.Clear();
        foreach (var (index, row) in _beakerRows)
        {
            if (index < solution.Value.Comp.Solution.Contents.Count)
                continue;

            row.Orphan();
            _toRemove.Add(index);
        }

        foreach (var remove in _toRemove)
        {
            _beakerRows.Remove(remove);
        }
    }

    private void UpdatePillBottles(Entity<RMCChemMasterComponent> chemMaster)
    {
        if (_window == null)
            return;

        var selectedBottles = chemMaster.Comp.SelectedBottles;
        var anySelected = selectedBottles.Count > 0;
        if (_container.TryGetContainer(Owner, chemMaster.Comp.PillBottleContainer, out var container) &&
            container.ContainedEntities.Count > 0)
        {
            var hasMultipleBottles = container.ContainedEntities.Count > 1;
            _window.SelectAllButton.Visible = hasMultipleBottles;

            if (hasMultipleBottles)
            {
                var allSelected = selectedBottles.Count == container.ContainedEntities.Count;
                _window.SelectAllButton.Text = allSelected
                    ? Loc.GetString("rmc-chem-master-deselect-all")
                    : Loc.GetString("rmc-chem-master-select-all");
            }

            _window.PillBottleColumnLabel.Margin = new Thickness(0, 3, 5, 0);
            _window.PillBottlesNoneLabel.Visible = false;

            foreach (var child in _window.PillBottlesContainer.Children)
            {
                if (child is not RMCChemMasterPillBottleRow row)
                    continue;

                row.LabelInput.Visible = anySelected;
                row.ColorButton.Disabled = !anySelected;
                break;
            }

            if (_lastPillBottleRows.SequenceEqual(container.ContainedEntities))
            {
                foreach (var contained in container.ContainedEntities)
                {
                    if (!_bottleRows.TryGetValue(contained, out var row))
                        continue;

                    var isSelected = selectedBottles.Contains(contained);
                    if (row.FillBottleButton is { Disposed: false })
                        row.FillBottleButton.Pressed = isSelected;

                    UpdatePillBottleFill(row, contained);
                    UpdatePillBottleName(row, contained);
                }

                return;
            }

            _lastPillBottleRows.Clear();
            _lastPillBottleRows.AddRange(container.ContainedEntities);

            _window.PillBottlesContainer.RemoveChildExcept(_window.PillBottlesNoneLabel);
            _bottleRows.Clear();

            for (var i = 0; i < container.ContainedEntities.Count; i++)
            {
                var contained = container.ContainedEntities[i];
                if (!EntMan.TryGetNetEntity(contained, out var netContained))
                    continue;

                var row = new RMCChemMasterPillBottleRow();
                row.FillBottleButton.Pressed = selectedBottles.Contains(contained);
                row.FillBottleButton.OnPressed += args => SendPredictedMessage(new RMCChemMasterPillBottleFillMsg(netContained.Value, args.Button.Pressed));

                UpdatePillBottleFill(row, contained);
                UpdatePillBottleName(row, contained);

                if (i == 0)
                {
                    row.LabelInput.OnTextEntered += OnLabelInputChanged;
                    row.LabelInput.OnFocusExit += OnLabelInputChanged;
                    row.ColorButton.OnPressed += _ =>
                    {
                        if (_colorPopup != null)
                        {
                            _colorPopup.OpenCentered();
                            return;
                        }

                        _colorPopup = new RMCChemMasterPopupWindow { Title = Loc.GetString("rmc-chem-master-pill-bottle-window-title") };
                        _colorPopup.OnClose += () => _colorPopup = null;
                        _colorPopup.OpenCentered();

                        for (var j = 0; j < chemMaster.Comp.PillCanisterTypes; j++)
                        {
                            var state = _sprite.GetState(new SpriteSpecifier.Rsi(chemMaster.Comp.PillCanisterRsi, $"pill_canister{j}"));
                            var button = new TextureButton
                            {
                                TextureNormal = state.Frame0,
                            };

                            var type = j;
                            button.OnPressed += _ =>
                            {
                                SendPredictedMessage(new RMCChemMasterPillBottleColorMsg((RMCPillBottleColors)type));
                                _colorPopup.Close();
                            };

                            _colorPopup.Grid.AddChild(button);
                        }
                    };

                    if (!anySelected)
                    {
                        row.LabelInput.Visible = false;
                        row.ColorButton.Disabled = true;
                    }

                    if (row.ColorView.Parent is { } colorViewParent)
                        colorViewParent.Margin = new Thickness();

                    row.ColorView.Orphan();
                    row.ColorButton.AddChild(row.ColorView);
                }
                else
                {
                    row.LabelInput.Visible = false;
                    row.ColorButton.Visible = false;
                }

                row.ColorView.SetEntity(contained);

                row.TransferButton.OnPressed += _ => SendPredictedMessage(new RMCChemMasterPillBottleTransferMsg(netContained.Value));
                row.EjectButton.OnPressed += _ => SendPredictedMessage(new RMCChemMasterPillBottleEjectMsg(netContained.Value));
                _window.PillBottlesContainer.AddChild(row);
                _bottleRows[contained] = row;
            }
        }
        else
        {
            _lastPillBottleRows.Clear();
            _window.PillBottleColumnLabel.Margin = new Thickness(0, 0, 5, 0);
            _window.PillBottlesContainer.RemoveChildExcept(_window.PillBottlesNoneLabel);
            _window.PillBottlesNoneLabel.Visible = true;
            _window.SelectAllButton.Visible = false;
        }
    }

    private void UpdateBuffer(Entity<RMCChemMasterComponent> chemMaster)
    {
        if (_window == null)
            return;

        _window.BufferModeButton.Text = chemMaster.Comp.BufferTransferMode switch
        {
            RMCChemMasterBufferMode.ToBeaker => Loc.GetString("rmc-chem-master-buffer-to-beaker"),
            RMCChemMasterBufferMode.ToDisposal => Loc.GetString("rmc-chem-master-buffer-to-disposal"),
            _ => _window.BufferModeButton.Text,
        };

        if (!_solution.TryGetSolution(Owner, chemMaster.Comp.BufferSolutionId, out var buffer) ||
            buffer.Value.Comp.Solution.Volume <= FixedPoint2.Zero)
        {
            _window.BufferEmptyLabel.Visible = true;
            _window.BufferAllButton.Visible = false;
            _window.BufferContainer.RemoveAllChildren();
            _bufferRows.Clear();
            return;
        }

        _window.BufferEmptyLabel.Visible = false;
        _window.BufferAllButton.Visible = true;
        for (var i = 0; i < buffer.Value.Comp.Solution.Contents.Count; i++)
        {
            var content = buffer.Value.Comp.Solution.Contents[i];
            if (!_bufferRows.TryGetValue(i, out var row))
            {
                row = CreateReagentRow(
                    chemMaster,
                    content,
                    setting => SendPredictedMessage(new RMCChemMasterBufferTransferMsg(content.Reagent.Prototype, setting))
                );

                _bufferRows[i] = row;
                _window.BufferContainer.AddChild(row);
            }

            UpdateReagentRow(
                row,
                content,
                setting => SendPredictedMessage(
                    new RMCChemMasterBufferTransferMsg(content.Reagent.Prototype, setting))
            );

            row.AllButton.ClearOnPressed();
            row.AllButton.OnPressed += _ => SendPredictedMessage(new RMCChemMasterBufferTransferMsg(content.Reagent.Prototype, content.Quantity));

            row.OnSubmit = null;
            row.OnSubmit += args =>
            {
                if (!double.TryParse(args.Text, out var amount))
                    return;

                SendPredictedMessage(
                    new RMCChemMasterBufferTransferMsg(content.Reagent.Prototype, FixedPoint2.New(amount)));
            };
        }

        _toRemove.Clear();
        foreach (var (index, row) in _bufferRows)
        {
            if (index < buffer.Value.Comp.Solution.Contents.Count)
                continue;

            row.Orphan();
            _toRemove.Add(index);
        }

        foreach (var remove in _toRemove)
        {
            _bufferRows.Remove(remove);
        }
    }

    private RMCChemMasterReagentRow CreateReagentRow(RMCChemMasterComponent chemMaster, ReagentQuantity reagent, Action<FixedPoint2> onTransfer)
    {
        var row = new RMCChemMasterReagentRow();
        foreach (var setting in chemMaster.TransferSettings)
        {
            var transferButton = new RMCChemMasterTransferButton { Amount = setting };
            row.TransferSettingsContainer.AddChild(transferButton);
        }

        UpdateReagentRow(row, reagent, onTransfer);
        return row;
    }

    private void UpdateReagentRow(RMCChemMasterReagentRow row, ReagentQuantity reagent, Action<FixedPoint2> onTransfer)
    {
        var name = reagent.Reagent.Prototype;
        if (_prototype.TryIndexReagent(name, out ReagentPrototype? reagentProto))
            name = reagentProto.LocalizedName;

        row.ReagentLabel.Text = Loc.GetString("rmc-chem-master-reagent-amount", ("name", name), ("amount", reagent.Quantity));
        foreach (var child in row.TransferSettingsContainer.Children)
        {
            if (child is not RMCChemMasterTransferButton button)
                continue;

            button.Button.Text = button.Amount.ToString();
            button.OnPressed = null;
            button.OnPressed += onTransfer;
        }
    }

    private void OnLabelInputChanged(LineEdit.LineEditEventArgs args)
    {
        if (args.Control.Root == null)
            return;

        SendPredictedMessage(new RMCChemMasterPillBottleLabelMsg(args.Text));
    }

    private void UpdatePillBottleFill(RMCChemMasterPillBottleRow row, EntityUid contained)
    {
        if (!EntMan.TryGetComponent(contained, out StorageComponent? storage))
            return;

        var total = 0;
        if (storage.Grid.TryFirstOrNull(out var firstGrid))
            total = firstGrid.Value.Width + 1;

        row.PillAmountLabel.Text = Loc.GetString("rmc-chem-master-pill-bottle-pills",
            ("amount", storage.StoredItems.Count),
            ("total", total));

        row.ColorView.SetEntity(contained);
    }

    private void UpdatePillBottleName(RMCChemMasterPillBottleRow row, EntityUid contained)
    {
        if (!EntMan.TryGetComponent(contained, out IconLabelComponent? label) ||
            label.LabelTextLocId == null ||
            !_localization.TryGetString(label.LabelTextLocId, out var labelStr, label.LabelTextParams.ToArray()) ||
            labelStr.Length <= 0)
        {
            row.NameLabel.Text = string.Empty;
            return;
        }

        if (labelStr.Length > 3)
            labelStr = labelStr[..3];

        row.NameLabel.Text = $"({labelStr})";
    }

    private void OpenPresetWindow(RMCChemMasterComponent chemMaster)
    {
        if (_presetWindow != null)
        {
            RefreshPresetsList(chemMaster);
            _presetWindow.OpenCentered();
            return;
        }

        _presetWindow = new RMCChemMasterPresetWindow { Title = Loc.GetString("rmc-chem-master-presets") };
        _presetWindow.OnClose += () =>
        {
            if (_presetWindow != null)
                _presetWindow.OnQuickAccessToggled -= OnQuickAccessToggled;

            _presetWindow = null;
            _presetEditWindow?.Close();
            _presetEditWindow = null;
            _presetColorPopup?.Close();
            _presetColorPopup = null;
            _presetPillTypePopup?.Close();
            _presetPillTypePopup = null;
        };

        _presetWindow.OnQuickAccessToggled += OnQuickAccessToggled;

        _presetWindow.OnReorderToggled += () =>
        {
            _presetWindow.RefreshPresetRows();
        };

        _presetWindow.CreateNewPresetButton.OnPressed += _ => OpenPresetEditWindow(chemMaster, null);

        RefreshPresetsList(chemMaster);
        _presetWindow.OpenCentered();
    }

    private void OpenPresetEditWindow(RMCChemMasterComponent chemMaster, RMCChemMasterPreset? existingPreset)
    {
        _presetEditWindow?.Close();

        var title = existingPreset != null
            ? Loc.GetString("rmc-chem-master-preset-edit-title", ("name", existingPreset.Name))
            : Loc.GetString("rmc-chem-master-preset-create-new");

        _presetEditWindow = new RMCChemMasterPresetEditWindow { Title = title };
        _presetEditWindow.OnClose += () =>
        {
            _presetEditWindow = null;
            _presetColorPopup?.Close();
            _presetColorPopup = null;
            _presetPillTypePopup?.Close();
            _presetPillTypePopup = null;
            _editingPresetName = null;
        };

        _hasPresetBottleColor = true;
        _hasPresetPillType = true;

        if (existingPreset != null)
        {
            _editingPresetName = existingPreset.Name;
            _selectedPresetBottleColor = existingPreset.BottleColor;
            _selectedPresetPillType = existingPreset.PillType;
            _presetEditWindow.LoadPreset(existingPreset);
        }
        else
        {
            _editingPresetName = null;
            _selectedPresetBottleColor = RMCPillBottleColors.Orange;
            _selectedPresetPillType = chemMaster.SelectedType;
            _presetEditWindow.Clear();
        }

        UpdatePresetEditBottleColorView(chemMaster);
        UpdatePresetEditPillTypeView(chemMaster);

        // Pill bottle color button
        _presetEditWindow.BottleColorButton.OnPressed += _ =>
        {
            if (_presetColorPopup != null)
            {
                _presetColorPopup.OpenCentered();
                return;
            }

            _presetColorPopup = new RMCChemMasterPopupWindow { Title = Loc.GetString("rmc-chem-master-pill-bottle-window-title") };
            _presetColorPopup.OnClose += () => _presetColorPopup = null;
            _presetColorPopup.OpenCentered();

            for (var j = 0; j < chemMaster.PillCanisterTypes; j++)
            {
                var state = _sprite.GetState(new SpriteSpecifier.Rsi(chemMaster.PillCanisterRsi, $"pill_canister{j}"));
                var button = new TextureButton { TextureNormal = state.Frame0 };

                var type = j;
                button.OnPressed += _ =>
                {
                    _selectedPresetBottleColor = (RMCPillBottleColors) type;
                    _hasPresetBottleColor = true;
                    UpdatePresetEditBottleColorView(chemMaster);
                    _presetColorPopup?.Close();
                };

                _presetColorPopup.Grid.AddChild(button);
            }

            // Add clear selection button at the bottom
            var clearButton = new Button
            {
                Text = "✕ " + Loc.GetString("rmc-chem-master-preset-clear-selection"),
                HorizontalExpand = true,
            };
            clearButton.OnPressed += _ =>
            {
                _hasPresetBottleColor = false;
                UpdatePresetEditBottleColorView(chemMaster);
                _presetColorPopup?.Close();
            };
            _presetColorPopup.ButtonContainer.AddChild(clearButton);
        };

        // Clear bottle color button
        _presetEditWindow.ClearBottleColorButton.OnPressed += _ =>
        {
            _hasPresetBottleColor = false;
            UpdatePresetEditBottleColorView(chemMaster);
        };

        // Pill type button
        _presetEditWindow.PillTypeButton.OnPressed += _ =>
        {
            if (_presetPillTypePopup != null)
            {
                _presetPillTypePopup.OpenCentered();
                return;
            }

            _presetPillTypePopup = new RMCChemMasterPopupWindow { Title = Loc.GetString("rmc-chem-master-pills-type-window-title") };
            _presetPillTypePopup.OnClose += () => _presetPillTypePopup = null;
            _presetPillTypePopup.OpenCentered();

            for (var j = 0; j < chemMaster.PillTypes; j++)
            {
                var state = _sprite.GetState(new SpriteSpecifier.Rsi(chemMaster.PillRsi.RsiPath, $"pill{j + 1}"));
                var button = new TextureButton { TextureNormal = state.Frame0 };

                var type = (uint) (j + 1);
                button.OnPressed += _ =>
                {
                    _selectedPresetPillType = type;
                    _hasPresetPillType = true;
                    UpdatePresetEditPillTypeView(chemMaster);
                    _presetPillTypePopup?.Close();
                };

                _presetPillTypePopup.Grid.AddChild(button);
            }

            // Add clear selection button at the bottom
            var clearButton = new Button
            {
                Text = "✕ " + Loc.GetString("rmc-chem-master-preset-clear-selection"),
                HorizontalExpand = true,
            };
            clearButton.OnPressed += _ =>
            {
                _hasPresetPillType = false;
                UpdatePresetEditPillTypeView(chemMaster);
                _presetPillTypePopup?.Close();
            };
            _presetPillTypePopup.ButtonContainer.AddChild(clearButton);
        };

        // Clear pill type button
        _presetEditWindow.ClearPillTypeButton.OnPressed += _ =>
        {
            _hasPresetPillType = false;
            UpdatePresetEditPillTypeView(chemMaster);
        };

        // Save button
        _presetEditWindow.SavePresetButton.OnPressed += _ =>
        {
            if (_presetEditWindow == null)
                return;

            var name = _presetEditWindow.PresetNameInput.Text;
            if (string.IsNullOrWhiteSpace(name))
                return;

            // Get existing quick access settings if editing
            int? quickAccessSlot = null;
            string? quickAccessLabel = null;
            if (_editingPresetName != null)
            {
                var existingPresetData = _presetManager.Presets.FirstOrDefault(p => p.Name == _editingPresetName);
                if (existingPresetData != null)
                {
                    quickAccessSlot = existingPresetData.QuickAccessSlot;
                    quickAccessLabel = existingPresetData.QuickAccessLabel;
                }
            }

            var preset = _presetEditWindow.GetPreset(
                _hasPresetBottleColor ? _selectedPresetBottleColor : RMCPillBottleColors.Orange,
                _hasPresetPillType ? _selectedPresetPillType : 1,
                quickAccessSlot,
                quickAccessLabel);

            if (_editingPresetName != null && !string.Equals(_editingPresetName, preset.Name, StringComparison.OrdinalIgnoreCase))
            {
                _presetManager.RemovePreset(_editingPresetName);
            }

            _presetManager.SavePreset(preset);
            RefreshPresetsList(chemMaster);
            UpdateQuickAccessBar();
            _presetEditWindow.Close();
        };

        _presetEditWindow.OpenCentered();
    }

    private void RefreshPresetsList(RMCChemMasterComponent chemMaster)
    {
        if (_presetWindow == null)
            return;

        _presetWindow.SavedPresetsContainer.RemoveAllChildren();
        _presetWindow.NoPresetsLabel.Visible = _presetManager.Presets.Count == 0;
        _presetRows.Clear();

        var usedSlots = new HashSet<int>();
        foreach (var p in _presetManager.Presets)
        {
            if (p.QuickAccessSlot != null)
                usedSlots.Add(p.QuickAccessSlot.Value);
        }

        for (var i = 0; i < _presetManager.Presets.Count; i++)
        {
            var preset = _presetManager.Presets[i];
            var index = i;
            var row = new RMCChemMasterPresetRow();
            _presetRows[preset.Name] = row;

            // Set pill bottle color icon
            var colorSpecifier = new SpriteSpecifier.Rsi(chemMaster.PillCanisterRsi, $"pill_canister{(int) preset.BottleColor}");
            row.BottleColorView.Texture = _sprite.Frame0(colorSpecifier);

            // Set pill color/type icon
            var pillSpecifier = new SpriteSpecifier.Rsi(chemMaster.PillRsi.RsiPath, $"pill{preset.PillType}");
            row.PillTypeView.Texture = _sprite.Frame0(pillSpecifier);

            // Preset button that applies the preset when pressed. PresetName "Label" [QuickAccessSlot]
            var buttonText = preset.Name;
            if (!string.IsNullOrWhiteSpace(preset.BottleLabel))
                buttonText += $" \"{preset.BottleLabel}\"";
            if (preset.QuickAccessSlot != null)
                buttonText += $" [Q{preset.QuickAccessSlot}]";
            row.ApplyButton.Text = buttonText;
            row.ApplyButton.OnPressed += _ => ApplyPresetDirectly(preset);

            // Quick access button
            row.SetHasQuickAccess(preset.QuickAccessSlot != null);
            row.QuickAccessButton.Text = "⚡";
            row.QuickAccessButton.OnPressed += _ =>
            {
                row.ToggleQuickAccessDropdown();
            };

            row.InitializeQuickAccessSlots(chemMaster.MaxQuickAccessSlots, usedSlots, preset.QuickAccessSlot);
            row.SetQuickAccessLabel(preset.QuickAccessLabel);

            row.OnQuickAccessSlotChanged += newSlot =>
            {
                var updatedPreset = new RMCChemMasterPreset
                {
                    Name = preset.Name,
                    BottleLabel = preset.BottleLabel,
                    BottleColor = preset.BottleColor,
                    PillType = preset.PillType,
                    UsePresetNameAsLabel = preset.UsePresetNameAsLabel,
                    QuickAccessSlot = newSlot,
                    QuickAccessLabel = preset.QuickAccessLabel,
                };
                _presetManager.SavePreset(updatedPreset);

                // Update the button text for this row
                var quickAccessButtonText = preset.Name;
                if (!string.IsNullOrWhiteSpace(preset.BottleLabel))
                    quickAccessButtonText += $" \"{preset.BottleLabel}\"";
                if (newSlot != null)
                    quickAccessButtonText += $" [Q{newSlot}]";
                row.ApplyButton.Text = quickAccessButtonText;
                row.SetHasQuickAccess(newSlot != null);

                // Recalculate used slots and update all rows' slot availability
                var newUsedSlots = new HashSet<int>();
                foreach (var p in _presetManager.Presets)
                {
                    if (p.QuickAccessSlot != null)
                        newUsedSlots.Add(p.QuickAccessSlot.Value);
                }
                foreach (var (_, presetRow) in _presetRows)
                {
                    presetRow.UpdateSlotAvailability(newUsedSlots);
                }

                UpdateQuickAccessBar();
            };

            row.OnQuickAccessLabelChanged += newLabel =>
            {
                var updatedPreset = new RMCChemMasterPreset
                {
                    Name = preset.Name,
                    BottleLabel = preset.BottleLabel,
                    BottleColor = preset.BottleColor,
                    PillType = preset.PillType,
                    UsePresetNameAsLabel = preset.UsePresetNameAsLabel,
                    QuickAccessSlot = preset.QuickAccessSlot,
                    QuickAccessLabel = newLabel,
                };
                _presetManager.SavePreset(updatedPreset);
                UpdateQuickAccessBar();
            };

            // Edit button
            row.EditButton.Text = "✏";
            row.EditButton.OnPressed += _ => OpenPresetEditWindow(chemMaster, preset);

            // Delete button
            row.DeleteButton.Text = "🗑";
            row.DeleteButton.OnPressed += _ => DeletePreset(preset.Name);

            // Move buttons mode
            row.MoveUpButton.Visible = _presetWindow.ShowReorder;
            row.MoveDownButton.Visible = _presetWindow.ShowReorder;
            row.MoveUpButton.Disabled = index == 0;
            row.MoveDownButton.Disabled = index == _presetManager.Presets.Count - 1;
            row.MoveUpButton.OnPressed += _ => MovePreset(preset.Name, -1, chemMaster);
            row.MoveDownButton.OnPressed += _ => MovePreset(preset.Name, 1, chemMaster);

            _presetWindow.SavedPresetsContainer.AddChild(row);
        }

        _presetWindow.InvalidateMeasure();
    }

    private void MovePreset(string name, int direction, RMCChemMasterComponent chemMaster)
    {
        if (_presetManager.MovePreset(name, direction))
        {
            RefreshPresetsList(chemMaster);
            UpdateQuickAccessBar();
        }
    }

    private void DeletePreset(string name)
    {
        if (!EntMan.TryGetComponent(Owner, out RMCChemMasterComponent? chemMaster))
            return;

        _presetManager.RemovePreset(name);
        RefreshPresetsList(chemMaster);
        UpdateQuickAccessBar();
    }

    private void UpdatePresetEditBottleColorView(RMCChemMasterComponent chemMaster)
    {
        if (_presetEditWindow == null)
            return;

        if (_hasPresetBottleColor)
        {
            var colorIndex = (int) _selectedPresetBottleColor;
            var specifier = new SpriteSpecifier.Rsi(chemMaster.PillCanisterRsi, $"pill_canister{colorIndex}");
            _presetEditWindow.BottleColorView.Texture = _sprite.Frame0(specifier);
            _presetEditWindow.BottleColorView.Visible = true;
        }
        else
        {
            _presetEditWindow.BottleColorView.Visible = false;
        }
    }

    private void UpdatePresetEditPillTypeView(RMCChemMasterComponent chemMaster)
    {
        if (_presetEditWindow == null)
            return;

        if (_hasPresetPillType)
        {
            var specifier = new SpriteSpecifier.Rsi(chemMaster.PillRsi.RsiPath, $"pill{_selectedPresetPillType}");
            _presetEditWindow.PillTypeView.Texture = _sprite.Frame0(specifier);
            _presetEditWindow.PillTypeView.Visible = true;
        }
        else
        {
            _presetEditWindow.PillTypeView.Visible = false;
        }
    }

    private void OnQuickAccessToggled()
    {
        if (_presetWindow == null)
            return;

        _showQuickAccess = _presetWindow.ShowQuickAccess;
        UpdateQuickAccessBar();
    }

    private void UpdateQuickAccessBar()
    {
        if (_window == null || _window.Disposed)
            return;

        _window.QuickAccessContainer.RemoveAllChildren();

        if (!_showQuickAccess)
        {
            _window.QuickAccessContainer.Visible = false;
            return;
        }

        var quickAccessPresets = _presetManager.Presets
            .Where(p => p.QuickAccessSlot != null)
            .OrderBy(p => p.QuickAccessSlot)
            .ToList();

        if (quickAccessPresets.Count == 0)
        {
            _window.QuickAccessContainer.Visible = false;
            return;
        }

        _window.QuickAccessContainer.Visible = true;

        foreach (var preset in quickAccessPresets)
        {
            var label = !string.IsNullOrWhiteSpace(preset.QuickAccessLabel)
                ? preset.QuickAccessLabel
                : preset.QuickAccessSlot?.ToString() ?? string.Empty;

            if (label.Length > 3)
                label = label[..3];

            var button = new Button
            {
                Text = label,
                ToolTip = preset.Name,
                MinWidth = 32,
                StyleClasses = { "OpenBoth" },
                ModulateSelfOverride = GetPillBottleButtonColor(preset.BottleColor),
            };

            var capturedPreset = preset;
            button.OnPressed += _ => ApplyPresetDirectly(capturedPreset);

            _window.QuickAccessContainer.AddChild(button);
        }
    }

    private void ApplyPresetDirectly(RMCChemMasterPreset preset)
    {
        SendPredictedMessage(new RMCChemMasterApplyPresetMsg(
            preset.Name,
            preset.BottleLabel,
            preset.BottleColor,
            preset.PillType,
            preset.UsePresetNameAsLabel));
    }

    private static Color GetPillBottleButtonColor(RMCPillBottleColors color)
    {
        return color switch
        {
            RMCPillBottleColors.Orange => new Color(255, 165, 0, 128),
            RMCPillBottleColors.Blue => new Color(0, 0, 255, 128),
            RMCPillBottleColors.Yellow => new Color(255, 255, 0, 128),
            RMCPillBottleColors.LightPurple => new Color(177, 156, 217, 128),
            RMCPillBottleColors.LightGrey => new Color(200, 200, 200, 128),
            RMCPillBottleColors.White => new Color(255, 255, 255, 128),
            RMCPillBottleColors.LightGreen => new Color(144, 238, 144, 128),
            RMCPillBottleColors.Cyan => new Color(0, 255, 255, 128),
            RMCPillBottleColors.Pink => new Color(255, 192, 203, 128),
            RMCPillBottleColors.Aquamarine => new Color(127, 255, 212, 128),
            RMCPillBottleColors.Grey => new Color(128, 128, 128, 128),
            RMCPillBottleColors.Red => new Color(255, 0, 0, 128),
            RMCPillBottleColors.Black => new Color(36, 16, 0, 128),
            _ => new Color(200, 200, 200, 128), // Default to LightGrey
        };
    }
}
