using Content.Client._RMC14.Xenonids.UI;
using Content.Shared._RMC14.Xenonids.ResinMark;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using System.Collections.Generic;
using System.Linq;

namespace Content.Client._RMC14.Xenonids.ResinMark;

[UsedImplicitly]
public sealed class XenoResinMarkBui : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly SpriteSystem _sprite;
    private readonly XenoResinMarkClientSystem _markClient;
    private EntProtoId _selectedType = "XenoPingMove";
    private NetEntity? _selectedPlacedMarker;
    private bool _canForceTrack;

    [ViewVariables]
    private XenoResinMarkWindow? _window;

    public XenoResinMarkBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _sprite = EntMan.System<SpriteSystem>();
        _markClient = EntMan.System<XenoResinMarkClientSystem>();
    }

    protected override void Open()
    {
        base.Open();
        _markClient.SetMenuOpen(Owner, true);
        EnsureWindow();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            _markClient.SetMenuOpen(Owner, false);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not XenoResinMarkBuiState s)
            return;

        _selectedType = s.SelectedType;
        _canForceTrack = s.CanForceTrack;
        var window = EnsureWindow();

        var selectedName = s.Types.FirstOrDefault(t => t.Id == s.SelectedType).Name;
        if (string.IsNullOrEmpty(selectedName))
            selectedName = s.SelectedType.Id;

        window.InstructionLabel.Text =
            "Select a marker type, then middle-click to place.";

        window.SelectedMarkLabel.Text = $"Type: {selectedName}";

        window.MarkTypeContainer.DisposeAllChildren();
        foreach (var type in s.Types)
        {
            var texture = _prototype.TryIndex<EntityPrototype>(type.Id, out var entity)
                ? _sprite.Frame0(entity)
                : null;

            var control = new XenoChoiceControl();
            control.Set(type.Name, texture);
            control.Button.ToggleMode = true;
            control.Button.Pressed = type.Id == s.SelectedType;
            control.Button.ToolTip = type.Description;
            control.Button.OnPressed += _ => SendPredictedMessage(new XenoResinMarkSelectTypeBuiMsg(type.Id));
            window.MarkTypeContainer.AddChild(control);
        }

        if (s.Marks.Count == 0)
        {
            _selectedPlacedMarker = null;
        }
        else if (_selectedPlacedMarker == null || s.Marks.All(m => m.Marker != _selectedPlacedMarker.Value))
        {
            _selectedPlacedMarker = s.Marks[0].Marker;
        }

        var selectedPlacedText = _selectedPlacedMarker is { } selectedMarker &&
                                 s.Marks.FirstOrDefault(m => m.Marker == selectedMarker) is { } selectedMark &&
                                 selectedMark.Marker == selectedMarker
            ? $"{selectedMark.Name} - {selectedMark.LocationName}"
            : "None";
        window.SelectedPlacedLabel.Text = $"Selected: {selectedPlacedText}";

        window.PlacedMarksContainer.DisposeAllChildren();
        foreach (var mark in s.Marks)
        {
            var control = new XenoChoiceControl();
            control.Set($"{mark.Name} ({mark.LocationName})", null);
            control.Button.ToggleMode = true;
            control.Button.Pressed = _selectedPlacedMarker == mark.Marker;
            control.Button.OnPressed += _ =>
            {
                _selectedPlacedMarker = mark.Marker;
                UpdatePlacedSelection(window, s.Marks);
            };

            window.PlacedMarksContainer.AddChild(control);
        }

        UpdatePlacedSelection(window, s.Marks);
    }

    private XenoResinMarkWindow EnsureWindow()
    {
        if (_window != null)
            return _window;

        _window = this.CreateWindow<XenoResinMarkWindow>();
        _window.WatchSelectedButton.OnPressed += _ =>
        {
            if (_selectedPlacedMarker is { } marker)
                SendPredictedMessage(new XenoResinMarkWatchBuiMsg(marker));
        };
        _window.DestroySelectedButton.OnPressed += _ =>
        {
            if (_selectedPlacedMarker is { } marker)
                SendPredictedMessage(new XenoResinMarkDestroyBuiMsg(marker));
        };
        _window.ForceTrackSelectedButton.OnPressed += _ =>
        {
            if (_selectedPlacedMarker is { } marker)
                SendPredictedMessage(new XenoResinMarkForceTrackBuiMsg(marker));
        };
        return _window;
    }

    private void UpdatePlacedSelection(XenoResinMarkWindow window, IReadOnlyList<XenoResinPlacedMark> marks)
    {
        foreach (var child in window.PlacedMarksContainer.Children)
        {
            if (child is not XenoChoiceControl choice)
                continue;

            choice.Button.Pressed = false;
        }

        var selected = _selectedPlacedMarker != null
            ? marks.FirstOrDefault(m => m.Marker == _selectedPlacedMarker.Value)
            : default;

        if (_selectedPlacedMarker != null && selected.Marker == _selectedPlacedMarker.Value)
            window.SelectedPlacedLabel.Text = $"Selected: {selected.Name} ({selected.LocationName})";
        else
            window.SelectedPlacedLabel.Text = "Selected: None";

        for (var i = 0; i < window.PlacedMarksContainer.ChildCount && i < marks.Count; i++)
        {
            if (window.PlacedMarksContainer.GetChild(i) is not XenoChoiceControl choice)
                continue;

            choice.Button.Pressed = _selectedPlacedMarker != null && marks[i].Marker == _selectedPlacedMarker.Value;
        }

        var hasSelection = _selectedPlacedMarker != null && selected.Marker == _selectedPlacedMarker.Value;
        window.WatchSelectedButton.Disabled = !hasSelection;
        window.DestroySelectedButton.Disabled = !hasSelection;
        window.ForceTrackSelectedButton.Disabled = !hasSelection || !_canForceTrack;
    }

}
