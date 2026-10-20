using System.Linq;
using Content.Client._RMC14.UserInterface;
using Content.Shared._RMC14.Chemistry.Generation;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared._RMC14.UserInterface;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using static Content.Client._RMC14.Chemistry.Generation.RMCSynthesisSimulatorWindow;

namespace Content.Client._RMC14.Chemistry.Generation;

[UsedImplicitly]
public sealed class RMCSynthesisSimulatorBui : BoundUserInterface, IRefreshableBui
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly ItemSlotsSystem _itemSlots;
    private readonly SharedRMCChemistryResearchSystem _research;
    private readonly SharedRMCSynthesisSimulatorSystem _simulator;
    private readonly RMCReagentSystem _rmcReagent;

    private RMCSynthesisSimulatorWindow? _window;
    private bool _overrideSafety;
    private int? _selectedRecipeIndex;

    public RMCSynthesisSimulatorBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
        _itemSlots = EntMan.System<ItemSlotsSystem>();
        _research = EntMan.System<SharedRMCChemistryResearchSystem>();
        _simulator = EntMan.System<SharedRMCSynthesisSimulatorSystem>();
        _rmcReagent = EntMan.System<RMCReagentSystem>();
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<RMCSynthesisSimulatorWindow>();

        _window.AmplifyButton.OnPressed += _ => SendPredictedMessage(new RMCSynthesisSimulatorSetModeBuiMsg(SynthesisMode.Amplify));
        _window.SuppressButton.OnPressed += _ => SendPredictedMessage(new RMCSynthesisSimulatorSetModeBuiMsg(SynthesisMode.Suppress));
        _window.RelateButton.OnPressed += _ => SendPredictedMessage(new RMCSynthesisSimulatorSetModeBuiMsg(SynthesisMode.Relate));
        _window.AddButton.OnPressed += _ => SendPredictedMessage(new RMCSynthesisSimulatorSetModeBuiMsg(SynthesisMode.Add));

        _window.EjectTargetButton.OnPressed += _ => SendPredictedMessage(new RMCSynthesisSimulatorEjectTargetBuiMsg());
        _window.EjectReferenceButton.OnPressed += _ => SendPredictedMessage(new RMCSynthesisSimulatorEjectReferenceBuiMsg());
        _window.SimulateButton.OnPressed += _ => SendPredictedMessage(new RMCSynthesisSimulatorSimulateBuiMsg());
        _window.CancelButton.OnPressed += _ => SendPredictedMessage(new RMCSynthesisSimulatorCancelSimulationBuiMsg());
        _window.OverrideButton.OnPressed += _ =>
        {
            _overrideSafety = _window.OverrideButton.Pressed;
            Colored(_window.OverrideButton, _overrideSafety ? OverrideActiveColor : ActionColor);
            Refresh();
        };
        _window.FinalizeButton.OnPressed += _ =>
        {
            if (_selectedRecipeIndex is { } index)
                SendPredictedMessage(new RMCSynthesisSimulatorPickRecipeBuiMsg(index));
        };

        Refresh();
    }

    public void Refresh()
    {
        if (_window is not { IsOpen: true } || !EntMan.TryGetComponent(Owner, out RMCSynthesisSimulatorComponent? comp))
            return;

        var hasResearch = _research.TryGetResearch(out var research);
        var credits = hasResearch ? research!.Value.Comp.Credits : 0;
        var clearance = hasResearch ? research!.Value.Comp.ClearanceLevel : 1;

        _window.SetCredits(credits, clearance);

        var locked = comp.Simulating || comp.Picking;
        var canCancel = locked;
        _window.SimulateButton.Visible = !canCancel;
        _window.CancelButton.Visible = canCancel;
        _window.SimulateButton.Disabled = locked;

        _window.AmplifyButton.Disabled = locked;
        _window.SuppressButton.Disabled = locked;
        _window.RelateButton.Disabled = locked;
        _window.AddButton.Disabled = locked;

        var targetItem = GetSlotItem(comp.TargetSlotId);
        var referenceItem = GetSlotItem(comp.ReferenceSlotId);

        var canEjectTarget = targetItem != null && !locked;
        var canEjectReference = referenceItem != null && !locked;
        _window.EjectTargetButton.Disabled = !canEjectTarget;
        _window.EjectReferenceButton.Disabled = !canEjectReference;

        HighlightMode(comp.Mode);

        var targetName = targetItem is { } t && EntMan.TryGetComponent(t, out RMCChemResearchReportComponent? targetReport)
            ? GetReportReagentName(targetReport)
            : null;

        var referenceName = referenceItem is { } r && EntMan.TryGetComponent(r, out RMCChemResearchReportComponent? referenceReport)
            ? GetReportReagentName(referenceReport)
            : null;

        _window.TargetIcon.SetEntity(targetItem);
        _window.ReferenceIcon.SetEntity(referenceItem);

        _window.TargetNameLabel.Text = targetName ?? "CHEMICAL DATA NOT INSERTED";
        Row(_window.TargetStatusPanel, _window.TargetNameLabel, targetItem != null ? GreenColor : RedColor);

        _window.ReferenceNameLabel.Text = referenceName ?? "CHEMICAL DATA NOT INSERTED";
        Row(_window.ReferenceStatusPanel, _window.ReferenceNameLabel, referenceItem != null ? GreenColor : RedColor);

        var cost = _simulator.GetCost((Owner, comp));
        _window.CostLabel.Text = $"ESTIMATED SIMULATING COST: {(cost > 0 ? cost.ToString() : "NULL")}";

        var predictedOverdose = _simulator.GetPredictedOverdose((Owner, comp));
        _window.OverdoseLabel.Text = $"OVERDOSE LEVEL AFTER SIMULATION: {(predictedOverdose is { } od ? od.ToString() : "NULL")}";

        var (statusText, statusColor) = comp.Simulating ? ("STATUS: SIMULATING", OrangeColor)
            : comp.Picking ? ("STATUS: ANALYSIS READY - PICK A RECIPE", GreenColor)
            : targetItem is null ? ("STATUS: NO TARGET INSERTED", RedColor)
            : ("STATUS: READY", TanColor);
        Row(_window.StatusPanel, _window.StatusLabel, statusText, statusColor);

        var showReference = comp.Mode is SynthesisMode.Relate or SynthesisMode.Add;

        _window.TargetPropertiesPanel.Visible = targetItem != null;
        _window.ReferenceHeader.Visible = showReference;
        _window.ReferenceStatusPanel.Visible = showReference;
        _window.ReferencePropertiesPanel.Visible = referenceItem != null && showReference;

        var targetSelectable = comp.Mode != SynthesisMode.Add;
        PopulateProperties(_window.TargetPropertiesBox, targetItem, comp.TargetProperty?.Id, comp.ReferenceProperty?.Id,
            targetSelectable, "This property conflicts with the selected reference property!", id =>
                SendPredictedMessage(new RMCSynthesisSimulatorSelectTargetPropertyBuiMsg(id)));

        if (showReference)
        {
            PopulateProperties(_window.ReferencePropertiesBox, referenceItem, comp.ReferenceProperty?.Id, comp.TargetProperty?.Id,
                true, "This property conflicts with the selected target property!", id =>
                    SendPredictedMessage(new RMCSynthesisSimulatorSelectReferencePropertyBuiMsg(id)));
        }

        _window.RecipePickerHeader.Visible = comp.Picking;
        _window.RecipePickerPanel.Visible = comp.Picking;
        if (comp.Picking && comp.RecipeCandidates is { } candidates)
            PopulateRecipeCandidates(candidates);
        else
            _selectedRecipeIndex = null;

        _window.FinalizeButton.Disabled = _selectedRecipeIndex is null;
    }

    private void HighlightMode(SynthesisMode mode)
    {
        SetModeButton(_window!.AmplifyButton, mode == SynthesisMode.Amplify);
        SetModeButton(_window.SuppressButton, mode == SynthesisMode.Suppress);
        SetModeButton(_window.RelateButton, mode == SynthesisMode.Relate);
        SetModeButton(_window.AddButton, mode == SynthesisMode.Add);
    }

    private string GetReportReagentName(RMCChemResearchReportComponent report)
    {
        if (!report.Completed)
            return "ANALYSIS INCOMPLETE";

        if (report.ReagentId is not { } reagentId)
            return "ANALYSIS INCOMPLETE";

        return _rmcReagent.TryIndex(reagentId, out var reagent) ? reagent.LocalizedName : reagentId.Id;
    }

    private EntityUid? GetSlotItem(string slotId)
    {
        return _itemSlots.GetItemOrNull(Owner, slotId);
    }

    private void PopulateProperties(
        BoxContainer box,
        EntityUid? item,
        string? selectedId,
        string? opposingSelectedId,
        bool selectable,
        string conflictTooltip,
        Action<ProtoId<ChemGeneratorPropertyPrototype>?> onSelect)
    {
        if (item is not { } itemUid || !EntMan.TryGetComponent(itemUid, out RMCChemResearchReportComponent? report))
        {
            box.RemoveAllChildren();
            return;
        }

        if (!report.Completed)
        {
            box.RemoveAllChildren();
            box.AddChild(new Label { Text = "Analysis incomplete", FontColorOverride = Color.Red });
            return;
        }

        var index = 0;
        foreach (var p in report.Properties)
        {
            var name = p.PropertyId;
            var description = string.Empty;
            var categoryColor = Color.White;
            if (_prototype.TryIndex<ChemGeneratorPropertyPrototype>(p.PropertyId, out var proto))
            {
                name = proto.ID;
                description = proto.Description;
                categoryColor = proto.Category switch
                {
                    ChemPropertyCategory.Positive => PropertyPositiveColor,
                    ChemPropertyCategory.Negative => PropertyNegativeColor,
                    _ => Color.White,
                };
            }

            var conflicting = opposingSelectedId != null && ChemPropertyRelations.AreConflicting(p.PropertyId, opposingSelectedId);

            PropertyButton button;
            if (index < box.ChildCount && box.GetChild(index) is PropertyButton existing)
            {
                button = existing;
            }
            else
            {
                button = new PropertyButton { HorizontalExpand = true, MinHeight = 28, ClipText = true };
                box.AddChild(button);
            }

            button.Text = $"{name} (lvl {p.Level})";
            button.ToggleMode = selectable;
            button.Pressed = p.PropertyId == selectedId;
            button.Disabled = selectable && conflicting && !_overrideSafety;
            button.ToolTip = conflicting && !_overrideSafety ? conflictTooltip : description;
            button.MouseFilter = selectable ? Control.MouseFilterMode.Stop : Control.MouseFilterMode.Ignore;
            button.PropertyId = p.PropertyId;
            button.OnSelect = selectable ? onSelect : null;

            var background = button.Disabled ? RedColor : button.Pressed ? GreenColor : DarkRowColor;
            button.Label.FontColorOverride = button.Disabled || button.Pressed ? Color.White : categoryColor;
            SetSelectableStyle(button, background);

            index++;
        }

        box.RemoveChildrenAfter(index);
    }

    private void PopulateRecipeCandidates(List<List<RecipeCandidateIngredient>> candidates)
    {
        var box = _window!.RecipeCandidatesBox;

        for (var i = 0; i < candidates.Count; i++)
        {
            var text = string.Join(", ", candidates[i].Select(c =>
            {
                var name = _rmcReagent.TryIndex(c.Id, out var reagent) ? reagent.LocalizedName : c.Id;
                return c.Catalyst ? $"{name} x{c.Amount} (catalyst)" : $"{name} x{c.Amount}";
            }));

            RecipeButton button;
            if (i < box.ChildCount && box.GetChild(i) is RecipeButton existing)
            {
                button = existing;
            }
            else
            {
                button = new RecipeButton { HorizontalExpand = true, MinHeight = 32, ClipText = true };
                box.AddChild(button);
            }

            button.Text = text;
            button.ToggleMode = true;
            button.Pressed = _selectedRecipeIndex == i;
            button.Index = i;
            button.OnSelect = index =>
            {
                _selectedRecipeIndex = index;
                Refresh();
            };

            button.Label.FontColorOverride = Color.White;
            SetSelectableStyle(button, button.Pressed ? GreenColor : DarkRowColor);
        }

        box.RemoveChildrenAfter(candidates.Count);
    }
}
