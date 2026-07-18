using Content.Client.Message;
using Content.Client.UserInterface.Controls;
using Content.Shared._RMC14.AlertLevel;
using Content.Shared._RMC14.Marines.Announce;
using Content.Shared._RMC14.Marines.ControlComputer;
using Content.Shared._RMC14.Marines.GroundsideOperations;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Overwatch;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Client._RMC14.Marines.GroundsideOperations;

[UsedImplicitly]
public sealed class GroundsideOperationsConsoleBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private GroundsideOperationsConsoleWindow? _window;
    private bool _confirmingEvacuation;
    private bool _confirmingGeneralQuarters;
    private bool _confirmingRedAlert;
    private NetEntity? _lastSelectedSquad;

    protected override void Open()
    {
        base.Open();
        if (_window != null)
            return;

        _window = this.CreateWindow<GroundsideOperationsConsoleWindow>();
        _window.OpenOverwatchButton.OnPressed += _ => SendPredictedMessage(new GroundsideOperationsOpenOverwatchMsg());
        _window.OpenCommandMonitorButton.OnPressed += _ => SendPredictedMessage(new GroundsideOperationsOpenOverwatchMsg());
        _window.OpenOrdnanceButton.OnPressed += _ => SendPredictedMessage(new GroundsideOperationsOpenOverwatchMsg());
        _window.TakeOperatorButton.OnPressed += _ => SendPredictedMessage(new OverwatchConsoleTakeOperatorBuiMsg());
        _window.StopOverwatchButton.OnPressed += _ => SendPredictedMessage(new OverwatchConsoleStopOverwatchBuiMsg());
        _window.TacticalMapButton.OnPressed += _ => SendPredictedMessage(new MarineCommunicationsOpenMapMsg());
        _window.EchoButton.OnPressed += _ => SendPredictedMessage(new MarineCommunicationsEchoSquadMsg());
        _window.AlertButton.OnPressed += _ => SendPredictedMessage(new MarineControlComputerAlertLevelMsg());
        _window.ShipAnnouncementButton.OnPressed += _ => SendPredictedMessage(new MarineControlComputerShipAnnouncementMsg());
        _window.MedalsButton.OnPressed += _ => SendPredictedMessage(new MarineControlComputerOpenMedalsPanelMsg());
        _window.GroundAnnouncementButton.OnPressed += _ =>
        {
            var message = Rope.Collapse(_window.GroundAnnouncementText.TextRope);
            SendPredictedMessage(new MarineCommunicationsComputerMsg(message));
            _window.GroundAnnouncementText.TextRope = new Rope.Leaf(string.Empty);
        };
        _window.HighCommandButton.OnPressed += _ =>
        {
            SendPredictedMessage(new GroundsideOperationsHighCommandMsg(_window.HighCommandMessage.Text));
            _window.HighCommandMessage.Text = string.Empty;
        };
        _window.MessageSquadButton.OnPressed += _ =>
        {
            SendPredictedMessage(new OverwatchConsoleSendMessageBuiMsg(_window.SquadMessage.Text));
            _window.SquadMessage.Text = string.Empty;
        };
        _window.MessageLeaderButton.OnPressed += _ =>
        {
            SendPredictedMessage(new OverwatchConsoleSendLeaderMessageBuiMsg(_window.SquadMessage.Text));
            _window.SquadMessage.Text = string.Empty;
        };
        _window.SetPrimaryObjectiveButton.OnPressed += _ =>
            SendPredictedMessage(new OverwatchConsoleSetSquadObjectiveBuiMsg(SquadObjectiveType.Primary, _window.PrimaryObjective.Text));
        _window.SetSecondaryObjectiveButton.OnPressed += _ =>
            SendPredictedMessage(new OverwatchConsoleSetSquadObjectiveBuiMsg(SquadObjectiveType.Secondary, _window.SecondaryObjective.Text));
        _window.ClearPrimaryObjectiveButton.OnPressed += _ =>
            SendPredictedMessage(new OverwatchConsoleClearSquadObjectiveBuiMsg(SquadObjectiveType.Primary));
        _window.ClearSecondaryObjectiveButton.OnPressed += _ =>
            SendPredictedMessage(new OverwatchConsoleClearSquadObjectiveBuiMsg(SquadObjectiveType.Secondary));
        _window.RemindPrimaryObjectiveButton.OnPressed += _ =>
            SendPredictedMessage(new OverwatchConsoleSetSquadObjectiveBuiMsg(SquadObjectiveType.Primary, _window.PrimaryObjective.Text));
        _window.RemindSecondaryObjectiveButton.OnPressed += _ =>
            SendPredictedMessage(new OverwatchConsoleSetSquadObjectiveBuiMsg(SquadObjectiveType.Secondary, _window.SecondaryObjective.Text));
        _window.RedAlertButton.OnPressed += _ => ConfirmRedAlert();
        _window.GeneralQuartersButton.OnPressed += _ => ConfirmGeneralQuarters();
        _window.EvacuationButton.OnPressed += _ => ConfirmEvacuation();
        Refresh();
    }

    public void Refresh()
    {
        if (_window is not { IsOpen: true } ||
            !EntMan.TryGetComponent(Owner, out GroundsideOperationsConsoleComponent? groundside))
        {
            return;
        }

        var alert = EntMan.System<RMCAlertLevelSystem>().Get();
        _window.AlertStatus.Text = alert == null
            ? Loc.GetString("rmc-goc-alert-unknown")
            : Loc.GetString("rmc-goc-alert-status", ("level", Loc.GetString($"rmc-alert-{alert.Value.ToString().ToLowerInvariant()}")));

        _window.LandingZonesContainer.DisposeAllChildren();
        foreach (var landingZone in groundside.LandingZones)
        {
            var button = new ConfirmButton
            {
                Text = landingZone.Name,
                StyleClasses = { "OpenBoth" },
            };
            button.OnPressed += _ => SendPredictedMessage(new MarineCommunicationsDesignatePrimaryLZMsg(landingZone.Id));
            _window.LandingZonesContainer.AddChild(button);
        }

        var hasOverwatch = EntMan.TryGetComponent(Owner, out OverwatchConsoleComponent? overwatch);
        RefreshSquadSummary(groundside, overwatch);
        _window.CommandMonitorStatus.Text = overwatch?.Operator is { } operatorName
            ? Loc.GetString("rmc-goc-command-monitor-active", ("operator", operatorName))
            : Loc.GetString("rmc-goc-command-monitor-idle");
        _window.StopOverwatchButton.Disabled = overwatch?.Squad == null;
        _window.OrdnanceStatus.SetMarkupPermissive(hasOverwatch && overwatch?.HasOrbital == true
            ? Loc.GetString("rmc-goc-ordnance-ready")
            : Loc.GetString("rmc-goc-ordnance-unavailable"));

        var canEcho = EntMan.TryGetComponent(Owner, out MarineCommunicationsComputerComponent? communications) && communications.CanCreateEcho;
        _window.EchoButton.Disabled = !canEcho;

        if (EntMan.TryGetComponent(Owner, out MarineControlComputerComponent? control))
        {
            _window.EvacuationButton.Disabled = !control.CanEvacuate;
            _window.EvacuationButton.Text = _confirmingEvacuation
                ? Loc.GetString("rmc-goc-confirm")
                : control.Evacuating
                    ? Loc.GetString("rmc-goc-cancel-evacuation")
                    : Loc.GetString("rmc-goc-initiate-evacuation");
        }

        _window.RedAlertButton.Text = _confirmingRedAlert
            ? Loc.GetString("rmc-goc-confirm")
            : Loc.GetString("rmc-goc-red-alert");
        _window.GeneralQuartersButton.Text = _confirmingGeneralQuarters
            ? Loc.GetString("rmc-goc-confirm-general-quarters")
            : Loc.GetString("rmc-goc-general-quarters");
    }

    private void RefreshSquadSummary(GroundsideOperationsConsoleComponent groundside, OverwatchConsoleComponent? overwatch)
    {
        if (_window == null)
            return;

        _window.SquadSummaryContainer.DisposeAllChildren();
        var selectedId = overwatch?.Squad;
        var selected = groundside.OverwatchSquads.FirstOrDefault(summary => summary.Id == selectedId);
        var hasSelectedSquad = selected.Id != default;

        foreach (var summary in groundside.OverwatchSquads)
        {
            var leader = summary.Leader ?? Loc.GetString("rmc-overwatch-console-none");
            var button = new Button
            {
                Text = Loc.GetString("rmc-goc-squad-summary", ("name", summary.Name), ("alive", summary.Alive), ("members", summary.Members), ("leader", leader)),
                ModulateSelfOverride = summary.Color,
                StyleClasses = { "OpenBoth" },
            };
            button.OnPressed += _ => SendPredictedMessage(new OverwatchConsoleSelectSquadBuiMsg(summary.Id));
            _window.SquadSummaryContainer.AddChild(button);
        }

        _window.MessageSquadButton.Disabled = !hasSelectedSquad;
        _window.MessageLeaderButton.Disabled = !hasSelectedSquad;
        _window.SetPrimaryObjectiveButton.Disabled = !hasSelectedSquad;
        _window.SetSecondaryObjectiveButton.Disabled = !hasSelectedSquad;
        _window.ClearPrimaryObjectiveButton.Disabled = !hasSelectedSquad;
        _window.ClearSecondaryObjectiveButton.Disabled = !hasSelectedSquad;
        _window.RemindPrimaryObjectiveButton.Disabled = !hasSelectedSquad;
        _window.RemindSecondaryObjectiveButton.Disabled = !hasSelectedSquad;

        if (!hasSelectedSquad)
        {
            _window.SelectedSquadSummary.Text = Loc.GetString("rmc-goc-no-squad-selected");
            return;
        }

        _window.SelectedSquadSummary.Text = Loc.GetString("rmc-goc-selected-squad-summary",
            ("name", selected.Name),
            ("leader", selected.Leader ?? Loc.GetString("rmc-overwatch-console-none")),
            ("primary", string.IsNullOrWhiteSpace(selected.PrimaryObjective) ? Loc.GetString("rmc-overwatch-console-none") : selected.PrimaryObjective),
            ("secondary", string.IsNullOrWhiteSpace(selected.SecondaryObjective) ? Loc.GetString("rmc-overwatch-console-none") : selected.SecondaryObjective));

        if (_lastSelectedSquad != selected.Id)
        {
            _lastSelectedSquad = selected.Id;
            _window.PrimaryObjective.Text = selected.PrimaryObjective;
            _window.SecondaryObjective.Text = selected.SecondaryObjective;
        }
    }

    private void ConfirmEvacuation()
    {
        if (_confirmingEvacuation)
            SendPredictedMessage(new MarineControlComputerToggleEvacuationMsg());

        _confirmingEvacuation = !_confirmingEvacuation;
        Refresh();
    }

    private void ConfirmGeneralQuarters()
    {
        if (_confirmingGeneralQuarters)
            SendPredictedMessage(new GroundsideOperationsGeneralQuartersMsg());

        _confirmingGeneralQuarters = !_confirmingGeneralQuarters;
        Refresh();
    }

    private void ConfirmRedAlert()
    {
        if (_confirmingRedAlert)
            SendPredictedMessage(new GroundsideOperationsRedAlertMsg());

        _confirmingRedAlert = !_confirmingRedAlert;
        Refresh();
    }
}
