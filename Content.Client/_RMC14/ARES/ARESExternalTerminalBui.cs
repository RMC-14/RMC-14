using Content.Shared._RMC14.ARES;
using Content.Shared._RMC14.ARES.ExternalTerminals;
using Content.Shared._RMC14.ARES.Logs;
using Content.Shared._RMC14.UserInterface;
using Content.Shared.Access;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.ARES;

public sealed class ARESExternalTerminalBui : BoundUserInterface, IRefreshableBui
{
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly ProtoId<AccessLevelPrototype> _logAccess = "RMCAccessLogs";
    private Menu _menu = Menu.HomeMenu;
    private Menu _previousMenu = Menu.HomeMenu;
    private int _logIndex = 0;
    private EntProtoId<RMCARESLogTypeComponent>? _logType;

    private enum Menu
    {
        HomeMenu,
        LogMenu,
    }

    public ARESExternalTerminalBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    private ARESExternalTerminalWindow? _window;

    public void Refresh()
    {
        if (_window is not { IsOpen: true })
            return;

        if (!EntMan.TryGetComponent(Owner, out RMCARESExternalTerminalComponent? terminal))
            return;

        UpdateMenu(terminal);
        RefreshLogin(terminal);
        UpdateLogCategory(terminal);
        RefreshLogs(terminal);
    }

    private void RefreshLogs(RMCARESExternalTerminalComponent component)
    {
        if (_window is not { IsOpen: true })
            return;

        _window.LogsContainer.RemoveAllChildren();
        foreach (var log in component.Logs)
        {
            var logLabel = new RichTextLabel();
            logLabel.Text = $"[font size= 12]{FormattedMessage.EscapeText(log)}[/font]";
            logLabel.Margin =  new Thickness(0, 0, 0,  (float)2.5);
            _window.LogsContainer.AddChild(logLabel);
        }

        var logStep = (float)component.LogsLength / 12;

        var logPageCount = Math.Ceiling(logStep);

        _window.LogLeft.Disabled = false;
        _window.LogRight.Disabled = false;

        if (_logIndex-1 < 0)
        {
            _window.LogLeft.Disabled = true;
        }
        if (_logIndex+1 >= logPageCount)
        {
            _window.LogRight.Disabled = true;
        }

        _window.LogNumber.Text = $"[font size= 16]{_logIndex + 1}/{logPageCount}";
    }

    private void UpdateMenu(RMCARESExternalTerminalComponent component)
    {
        if (_window is not { IsOpen: true })
            return;

        _window.LogMenu.Visible = false;
        _window.HomeMenu.Visible = false;

        if (_menu == Menu.HomeMenu)
        {
            _window.HomeMenu.Visible = true;
        }
        else if (_menu == Menu.LogMenu)
        {
            _window.LogMenu.Visible = true;
        }
    }

    public void UpdateLogCategory(RMCARESExternalTerminalComponent terminal)
    {
        if (_window is not { IsOpen: true })
            return;


        if (terminal.ShowsLogs && terminal.LoggedIn && terminal.Accesses.Contains(_logAccess) && _window.LogCategory.ButtonContainer.ChildCount == 0)
        {
            _window.LogCategory.Visible = true;
            foreach (var logType in terminal.ShownLogs)
            {
                if (!_prototype.TryIndex<EntityPrototype>(logType, out var proto))
                    return;
                var logComp = logType.Get(_prototype, _compFactory);
                var logName = proto.Name;
                var logPerm = logComp.Permissions;
                var logColor = logComp.Color;

                var button = new ARESExternalTerminalButton();

                button.logButton.Text = logName;
                button.ModulateSelfOverride = logColor;

                button.logButton.OnPressed += _ =>
                {
                    SendPredictedMessage(new RMCARESExternalShowLogs(logType, 0));
                    _logType = logType;
                    _logIndex = 0;
                    _previousMenu = _menu;
                    _menu = Menu.LogMenu;
                    _window.LogsName.Text = $"[font size=16]Logs: {logName}[/font]";
                    _window.LogsDescription.Text = $"[font size=12]Description: {proto.Description}[/font]";
                    Refresh();
                };

                _window.LogCategory.ButtonContainer.AddChild(button);
            }
        }
        else if (!terminal.LoggedIn)
        {
            _window.LogCategory.Visible = false;
            _window.LogCategory.ButtonContainer.RemoveAllChildren();
        }
    }

    private void RefreshLogin(RMCARESExternalTerminalComponent terminal)
    {
        if (_window is not { IsOpen: true })
            return;

        if (!terminal.LoggedIn)
        {
            _window.MainProgram.Visible = false;
            _window.SignedInUser.Text = "";
            _window.LoginScreen.Visible = true;
            return;
        }

        var text = $"[font size=15]{FormattedMessage.EscapeText(terminal.LoggedInUser)}[/font]";
        _window.SignedInUser.Text =  text;
        _window.MainProgram.Visible = true;
        _window.LoginScreen.Visible = false;
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<ARESExternalTerminalWindow>();

        _window.SignIn.OnPressed += _ =>
        {
            SendPredictedMessage(new RMCARESExternalLogin());
            _previousMenu = Menu.HomeMenu;
            _menu = Menu.HomeMenu;
            Refresh();
        };

        _window.LogOut.OnPressed += _ =>
        {
            SendPredictedMessage(new RMCARESExternalLogout());
            Refresh();
        };

        _window.Back.OnPressed += _ =>
        {
            _menu = _previousMenu;
            _previousMenu = Menu.HomeMenu;
            Refresh();
        };

        _window.Home.OnPressed += _ =>
        {
            _previousMenu = _menu;
            _menu = Menu.HomeMenu;
            Refresh();
        };

        _window.LogLeft.OnPressed += _ =>
        {
            _window.LogCategory.ButtonContainer.RemoveAllChildren();
            _logIndex--;
            SendPredictedMessage(new RMCARESExternalShowLogs(_logType, _logIndex));
            Refresh();
        };

        _window.LogRight.OnPressed += _ =>
        {
            _window.LogCategory.ButtonContainer.RemoveAllChildren();
            _logIndex++;
            SendPredictedMessage(new RMCARESExternalShowLogs(_logType, _logIndex));
            Refresh();
        };

        Refresh();
    }

}
