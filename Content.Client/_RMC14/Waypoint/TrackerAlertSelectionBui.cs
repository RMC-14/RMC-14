using System.Linq;
using Content.Client.UserInterface.Controls;
using Content.Shared._RMC14.Waypoint;
using Content.Shared.Alert;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Waypoint;

[UsedImplicitly]
public sealed class TrackerAlertSelectionBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private ChooseTrackerAlertWindow? _window;

    protected override void Open()
    {
        EnsureWindow();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _window?.Dispose();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not TrackerAlertBuiState s)
            return;

        _window = EnsureWindow();
        _window.TrackerList.PopulateList(s.Entries
            .Select(e => new TrackerAlertListData(e.Entity, e.Name, e.Id, e.AlertPrototype))
            .ToList());
    }

    private ChooseTrackerAlertWindow EnsureWindow()
    {
        if (_window != null)
            return _window;

        _window = new ChooseTrackerAlertWindow();
        _window.OnClose += Close;
        _window.OpenCentered();
        _window.TrackerList.GenerateItem += OnTrackerListGenerateItem;
        _window.TrackerList.ItemPressed += OnTrackerListItemPressed;
        return _window;
    }

    private void OnTrackerListGenerateItem(ListData d, ListContainerButton button)
    {
        if (d is not TrackerAlertListData data)
            return;

        button.AddChild(new ChooseTrackerAlertEntry(data.Name, data.Id));
        button.AddStyleClass(ListContainer.StyleClassListContainerButton);
    }

    private void OnTrackerListItemPressed(BaseButton.ButtonEventArgs args, ListData d)
    {
        if (d is not TrackerAlertListData data)
            return;

        SendPredictedMessage(new TrackerAlertBuiMsg(data.EntityUid, data.AlertProto));
    }
}

public sealed record TrackerAlertListData(
    NetEntity EntityUid,
    string Name,
    EntProtoId? Id,
    ProtoId<AlertPrototype> AlertProto) : ListData;
