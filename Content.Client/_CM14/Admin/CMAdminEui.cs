using System.Numerics;
using Content.Client.Eui;
using Content.Shared._CM14.Admin;
using Content.Shared.Eui;
using JetBrains.Annotations;
using static Robust.Client.UserInterface.Controls.BaseButton;
using static Robust.Client.UserInterface.Controls.ItemList;
using static Robust.Client.UserInterface.Controls.LineEdit;

namespace Content.Client._CM14.Admin;

[UsedImplicitly]
public sealed class CMAdminEui : BaseEui
{
    private CMAdminWindow _adminWindow = default!;
    private CMCreateHiveWindow? _createHiveWindow;

    public override void Opened()
    {
        _adminWindow = new CMAdminWindow();

        _adminWindow.XenoTab.HiveList.OnItemSelected += OnHiveSelected;
        _adminWindow.XenoTab.CreateHiveButton.OnPressed += OnCreateHivePressed;

        _adminWindow.OpenCentered();
    }

    private void OnHiveSelected(ItemListSelectedEventArgs args)
    {
        var item = args.ItemList[args.ItemIndex];
        var msg = new CMAdminChangeHiveMessage((Hive) item.Metadata!);
        SendMessage(msg);
    }

    private void OnCreateHivePressed(ButtonEventArgs args)
    {
        if (_createHiveWindow != null)
        {
            _createHiveWindow.RecenterWindow(new Vector2(0.5f, 0.5f));
            return;
        }

        _createHiveWindow = new CMCreateHiveWindow();
        _createHiveWindow.OnClose += OnCreateHiveClosed;
        _createHiveWindow.HiveName.OnTextEntered += OnCreateHiveEntered;

        _createHiveWindow.OpenCentered();
    }

    private void OnCreateHiveClosed()
    {
        _createHiveWindow?.Dispose();
        _createHiveWindow = null;
    }

    private void OnCreateHiveEntered(LineEditEventArgs args)
    {
        var msg = new CMAdminCreateHiveMessage(args.Text);
        SendMessage(msg);
        _createHiveWindow?.Dispose();
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not CMAdminEuiState s)
            return;

        foreach (var hive in s.Hives)
        {
            var list = _adminWindow.XenoTab.HiveList;
            list.Add(new Item(list)
            {
                Text = hive.Name,
                Metadata = hive
            });
        }
    }

    public override void Closed()
    {
        _adminWindow.Dispose();
    }
}
