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

    private RMCChemMasterWindow? _window;
    private RMCChemMasterPopupWindow? _colorPopup;
    private FixedPoint2? _lastBottleAmount;

    private readonly List<EntityUid> _lastPillBottleRows = new();
    private readonly Dictionary<EntityUid, RMCChemMasterPillBottleRow> _bottleRows = new();
    private readonly Dictionary<int, RMCChemMasterReagentRow> _beakerRows = new();
    private readonly Dictionary<int, RMCChemMasterReagentRow> _bufferRows = new();
    private readonly List<int> _toRemove = new();

    public RMCChemMasterBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _container = EntMan.System<ContainerSystem>();
        _itemSlots = EntMan.System<ItemSlotsSystem>();
        _solution = EntMan.System<SolutionContainerSystem>();
        _sprite = EntMan.System<SpriteSystem>();
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

        var anySelected = chemMaster.Comp.SelectedBottles.Count > 0;
        if (_container.TryGetContainer(Owner, chemMaster.Comp.PillBottleContainer, out var container) &&
            container.ContainedEntities.Count > 0)
        {
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
                var fillBottles = chemMaster.Comp.SelectedBottles;
                row.FillBottleButton.Pressed = fillBottles.Contains(contained);
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
}
