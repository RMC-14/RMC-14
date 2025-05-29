using System.Linq;
using Content.Client._RMC14.UserInterface;
using Content.Client.Chemistry.Containers.EntitySystems;
using Content.Shared._RMC14.Chemistry.ChemMaster;
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
        _window.BufferModeButton.OnPressed += _ =>
            SendPredictedMessage(new RMCChemMasterBufferModeMsg(chemMaster.BufferTransferMode.NextWrap()));

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

            button.Button.OnPressed += _ => SendPredictedMessage(new RMCChemMasterSetPillTypeMsg(type));
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

        var type = chemMaster.SelectedType - 1;
        if (type >= 0 &&
            chemMaster.SelectedType < _window.PillTypeContainer.ChildCount &&
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

        if (_itemSlots.TryGetSlot(Owner, chemMaster.Comp.BeakerSlot, out var slot) &&
            slot.ContainerSlot?.ContainedEntity is { } beaker &&
            EntMan.TryGetComponent(beaker, out FitsInDispenserComponent? fits) &&
            _solution.TryGetSolution(beaker, fits.Solution, out var solution))
        {
            _window.BeakerLabel.Text = Loc.GetString("rmc-chem-master-beaker-amount",
                ("amount", solution.Value.Comp.Solution.Volume));

            _window.BeakerEmptyLabel.Visible = solution.Value.Comp.Solution.Volume <= FixedPoint2.Zero;
            _window.BeakerContentsContainer.RemoveAllChildren();

            foreach (var content in solution.Value.Comp.Solution.Contents)
            {
                var row = CreateReagentRow(
                    chemMaster,
                    content,
                    setting => SendPredictedMessage(new RMCChemMasterBeakerTransferMsg(setting))
                );

                row.AllButton.OnPressed += _ => SendPredictedMessage(new RMCChemMasterBeakerTransferAllMsg());
                row.CustomEdit.OnTextEntered += args =>
                {
                    if (!double.TryParse(args.Text, out var amount))
                        return;

                    SendPredictedMessage(new RMCChemMasterBeakerTransferMsg(FixedPoint2.New(amount)));
                };

                _window.BeakerContentsContainer.AddChild(row);
            }
        }
        else
        {
            _window.BeakerLabel.Text = Loc.GetString("rmc-chem-master-beaker-none");
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
            _window.PillBottleColumnLabel.Margin = new Thickness(0, 6, 5, 0);
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
                return;

            _lastPillBottleRows.Clear();
            _lastPillBottleRows.AddRange(container.ContainedEntities);

            _window.PillBottlesContainer.RemoveChildExcept(_window.PillBottlesNoneLabel);

            for (var i = 0; i < container.ContainedEntities.Count; i++)
            {
                var contained = container.ContainedEntities[i];
                if (!EntMan.TryGetNetEntity(contained, out var netContained))
                    continue;

                var row = new RMCChemMasterPillBottleRow();
                var fillBottles = chemMaster.Comp.SelectedBottles;
                row.FillBottleButton.Pressed = fillBottles.Contains(contained);
                row.FillBottleButton.OnPressed += args => SendPredictedMessage(new RMCChemMasterPillBottleFillMsg(netContained.Value, args.Button.Pressed));

                if (EntMan.TryGetComponent(contained, out StorageComponent? storage))
                {
                    var total = 0;
                    if (storage.Grid.TryFirstOrNull(out var firstGrid))
                        total = firstGrid.Value.Width + 1;

                    row.PillAmountLabel.Text = Loc.GetString("rmc-chem-master-pill-bottle-pills",
                        ("amount", storage.StoredItems.Count),
                        ("total", total));

                    row.ColorView.SetEntity(contained);
                }

                if (EntMan.TryGetComponent(contained, out IconLabelComponent? label) &&
                    label.LabelTextLocId != null &&
                    _localization.TryGetString(label.LabelTextLocId, out var labelStr, label.LabelTextParams.ToArray()) &&
                    labelStr.Length > 0)
                {
                    if (labelStr.Length > 3)
                        labelStr = labelStr[..3];

                    row.NameLabel.Text = $"({labelStr})";
                }

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

                        for (var j = 0; j < chemMaster.Comp.PillTypes; j++)
                        {
                            var state = _sprite.GetState(new SpriteSpecifier.Rsi(chemMaster.Comp.PillCanisterRsi, $"pill_canister{j + 1}"));
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

        _window.BufferContainer.RemoveChildExcept(_window.BufferEmptyLabel);
        if (_solution.TryGetSolution(Owner, chemMaster.Comp.BufferSolutionId, out var buffer) &&
            buffer.Value.Comp.Solution.Volume > FixedPoint2.Zero)
        {
            _window.BufferEmptyLabel.Visible = false;

            foreach (var content in buffer.Value.Comp.Solution.Contents)
            {
                var row = CreateReagentRow(
                    chemMaster,
                    content,
                    setting => SendPredictedMessage(new RMCChemMasterBufferTransferMsg(setting))
                );

                row.AllButton.OnPressed += _ => SendPredictedMessage(new RMCChemMasterBufferTransferAllMsg());
                row.CustomEdit.OnTextEntered += args =>
                {
                    if (!double.TryParse(args.Text, out var amount))
                        return;

                    SendPredictedMessage(new RMCChemMasterBufferTransferMsg(FixedPoint2.New(amount)));
                };

                _window.BufferContainer.AddChild(row);
            }
        }
        else
        {
            _window.BufferEmptyLabel.Visible = true;
        }
    }

    private RMCChemMasterReagentRow CreateReagentRow(RMCChemMasterComponent chemMaster, ReagentQuantity reagent, Action<FixedPoint2> onTransfer)
    {
        var row = new RMCChemMasterReagentRow();
        var name = reagent.Reagent.Prototype;
        if (_prototype.TryIndex(name, out ReagentPrototype? reagentProto))
            name = reagentProto.LocalizedName;

        row.ReagentLabel.Text = Loc.GetString("rmc-chem-master-reagent-amount", ("name", name), ("amount", reagent.Quantity));
        foreach (var setting in chemMaster.TransferSettings)
        {
            var transferButton = new Button { StyleClasses = { "OpenBoth" } };
            transferButton.Text = setting.ToString();
            transferButton.OnPressed += _ => onTransfer(setting);
            row.TransferSettingsContainer.AddChild(transferButton);
        }

        return row;
    }

    private void OnLabelInputChanged(LineEdit.LineEditEventArgs args)
    {
        if (args.Control.Root == null)
            return;

        SendPredictedMessage(new RMCChemMasterPillBottleLabelMsg(args.Text));
    }
}
