using Content.Client._RMC14.UserInterface.Crt;
using Content.Shared._RMC14.Marines.Announce;
using Content.Shared._RMC14.Marines.ControlComputer;
using Content.Shared._RMC14.TacticalMap;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.Marines.CommandTablet;

[UsedImplicitly]
public sealed class CommandTabletBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [Dependency] private readonly IGameTiming _timing = default!;

    private CommandTabletWindow? _window;
    private bool _confirmingEvacuation;
    private bool? _lastEvacuating;

    protected override void Open()
    {
        base.Open();
        if (_window != null)
            return;

        _window = this.CreateWindow<CommandTabletWindow>();
        _window.OnTimeRefresh += Refresh;
        _window.AnnouncementButton.OnPressed += _ =>
            SendPredictedMessage(new MarineCommunicationsOpenAnnouncementMsg());
        _window.MedalButton.OnPressed += _ =>
            SendPredictedMessage(new MarineControlComputerOpenMedalsPanelMsg());
        _window.TacticalMapButton.OnPressed += _ =>
            SendPredictedMessage(new MarineCommunicationsOpenMapMsg());
        _window.EvacuationButton.OnPressed += _ => ConfirmEvacuation();
        Refresh();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        Refresh();
    }

    public void Refresh()
    {
        if (_window is not { IsOpen: true } ||
            !EntMan.TryGetComponent(Owner, out MarineCommunicationsComputerComponent? communications))
        {
            return;
        }

        RefreshAnnouncement(communications);
        _window.MedalButton.Visible = communications.CanGiveMedals;
        _window.TacticalMapButton.Visible = EntMan.HasComponent<TacticalMapComputerComponent>(Owner);

        if (!communications.CanInitiateEvac ||
            !EntMan.TryGetComponent(Owner, out MarineControlComputerComponent? control))
        {
            _window.EvacuationSection.Visible = false;
            return;
        }

        _window.EvacuationSection.Visible = true;
        RefreshEvacuation(control);
    }

    private void RefreshAnnouncement(MarineCommunicationsComputerComponent communications)
    {
        if (_window == null)
            return;

        var remaining = communications.LastAnnouncement is { } last
            ? last + communications.Cooldown - _timing.CurTime
            : TimeSpan.Zero;
        var coolingDown = remaining > TimeSpan.Zero;
        _window.AnnouncementButton.Text = coolingDown
            ? Loc.GetString(
                "rmc-command-tablet-announcement-cooldown",
                ("seconds", Math.Max(1, (int) Math.Ceiling(remaining.TotalSeconds))))
            : Loc.GetString("rmc-command-tablet-make-announcement");
        _window.AnnouncementButton.Disabled = coolingDown;
    }

    private void RefreshEvacuation(MarineControlComputerComponent control)
    {
        if (_window == null)
            return;

        if (_lastEvacuating is { } lastEvacuating && lastEvacuating != control.Evacuating)
            _confirmingEvacuation = false;

        _lastEvacuating = control.Evacuating;
        if (!control.CanEvacuate)
            _confirmingEvacuation = false;

        _window.EvacuationWarning.Visible = !control.CanEvacuate;
        _window.EvacuationButton.Text = _confirmingEvacuation
            ? Loc.GetString(control.Evacuating
                ? "rmc-command-tablet-confirm-cancel-evacuation"
                : "rmc-command-tablet-confirm-evacuation")
            : Loc.GetString(control.Evacuating
                ? "rmc-command-tablet-cancel-evacuation"
                : "rmc-command-tablet-initiate-evacuation");
        _window.EvacuationButton.IconState = control.CanEvacuate
            ? RMCCrtIcons.DoorOpen
            : RMCCrtIcons.Ban;
        _window.EvacuationButton.Disabled = !control.CanEvacuate;
    }

    private void ConfirmEvacuation()
    {
        if (!EntMan.TryGetComponent(Owner, out MarineControlComputerComponent? control) ||
            !control.CanEvacuate)
        {
            _confirmingEvacuation = false;
            Refresh();
            return;
        }

        if (_confirmingEvacuation)
        {
            SendPredictedMessage(new MarineCommunicationsToggleEvacuationMsg());
            _confirmingEvacuation = false;
        }
        else
        {
            _confirmingEvacuation = true;
        }

        Refresh();
    }
}
