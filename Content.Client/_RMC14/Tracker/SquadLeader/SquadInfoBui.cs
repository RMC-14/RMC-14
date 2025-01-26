﻿using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Tracker.SquadLeader;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Tracker.SquadLeader;

[UsedImplicitly]
public sealed class SquadInfoBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private SquadInfoWindow? _window;

    protected override void Open()
    {
        _window = this.CreateWindow<SquadInfoWindow>();
        Refresh();
    }

    public void Refresh()
    {
        if (_window is not { IsOpen: true })
            return;

        if (!EntMan.TryGetComponent(Owner, out SquadLeaderTrackerComponent? tracker))
            return;

        var isSquadLeader = EntMan.HasComponent<SquadLeaderComponent>(Owner);
        var squadLeader = tracker.Fireteams.SquadLeader == null
            ? Loc.GetString("rmc-squad-info-squad-leader-none")
            : Loc.GetString("rmc-squad-info-squad-leader-name", ("leader", tracker.Fireteams.SquadLeader));
        _window.SquadLeaderLabel.Text = squadLeader;
        _window.ChangeTrackerButton.OnPressed += _ => SendPredictedMessage(new SquadLeaderTrackerChangeTrackedMsg());

        _window.FireteamsContainer.DisposeAllChildren();
        for (var i = 0; i < tracker.Fireteams.Fireteams.Length; i++)
        {
            var fireteam = tracker.Fireteams.Fireteams[i];
            if (fireteam?.Members is not { Count: > 0 })
                continue;

            var container = new SquadFireteamContainer();
            var teamLeader = fireteam.Leader == null
                ? Loc.GetString("rmc-squad-info-team-leader-none")
                : Loc.GetString("rmc-squad-info-team-leader-name", ("leader", fireteam.Leader.Value.Name));
            container.LeaderLabel.Text = teamLeader;

            var fireatemIndex = i ;
            container.RemoveLeaderButton.OnPressed +=
                _ => SendPredictedMessage(new SquadLeaderTrackerDemoteFireteamLeaderMsg(fireatemIndex));
            container.RemoveLeaderButton.Visible = fireteam.Leader != null && isSquadLeader;
            container.FireteamLabel.Text = Loc.GetString("rmc-squad-info-fireteam", ("fireteam", i + 1));

            foreach (var (_, member) in fireteam.Members)
            {
                if (member.Id == fireteam.Leader?.Id)
                    continue;

                var row = new SquadInfoRow();
                row.NameLabel.Text = $"[bold]{FormattedMessage.EscapeText(member.Name)}[/bold]";
                container.MembersContainer.AddChild(row);

                var promoteButton = new Button
                {
                    MaxWidth = 25,
                    MaxHeight = 25,
                    VerticalAlignment = Control.VAlignment.Top,
                    StyleClasses = { "OpenBoth" },
                    Text = "^",
                    TextAlign = Label.AlignMode.Center,
                    ToolTip = Loc.GetString("rmc-squad-info-promote-team-leader"),
                };

                promoteButton.Visible = isSquadLeader;
                promoteButton.OnPressed += _ =>
                    SendPredictedMessage(new SquadLeaderTrackerPromoteFireteamLeaderMsg(member.Id));

                row.ActionsContainer.AddChild(promoteButton);

                var unassignButton = new Button
                {
                    MaxWidth = 25,
                    MaxHeight = 25,
                    VerticalAlignment = Control.VAlignment.Top,
                    StyleClasses = { "OpenBoth" },
                    Text = "x",
                    TextAlign = Label.AlignMode.Center,
                    ToolTip = Loc.GetString("rmc-squad-info-unassign-fireteam"),
                };

                unassignButton.Visible = isSquadLeader;
                unassignButton.OnPressed += _ =>
                    SendPredictedMessage(new SquadLeaderTrackerUnassignFireteamMsg(member.Id));

                row.ActionsContainer.AddChild(unassignButton);
            }

            _window.FireteamsContainer.AddChild(container);
        }

        var unassignedContainer = new SquadFireteamContainer();
        unassignedContainer.LeaderContainer.Visible = false;
        unassignedContainer.RemoveLeaderButton.Visible = false;
        unassignedContainer.FireteamLabel.Text = Loc.GetString("rmc-squad-info-unassigned");
        unassignedContainer.ActionsLabel.Text = Loc.GetString("rmc-squad-info-actions");
        foreach (var (_, unassigned) in tracker.Fireteams.Unassigned)
        {
            if (tracker.Fireteams.SquadLeaderId == unassigned.Id)
                continue;

            var row = new SquadInfoRow();
            row.NameLabel.Text = $"[bold]{FormattedMessage.EscapeText(unassigned.Name)}[/bold]";
            unassignedContainer.MembersContainer.AddChild(row);

            for (var i = 0; i < tracker.Fireteams.Fireteams.Length; i++)
            {
                var button = new Button
                {
                    MaxWidth = 25,
                    MaxHeight = 25,
                    VerticalAlignment = Control.VAlignment.Top,
                    StyleClasses = { "OpenBoth" },
                    Text = $"{i + 1}",
                };

                var fireteamIndex = i;
                button.Visible = isSquadLeader;
                button.OnPressed += _ =>
                    SendPredictedMessage(new SquadLeaderTrackerAssignFireteamMsg(unassigned.Id, fireteamIndex));

                row.ActionsContainer.AddChild(button);
            }
        }

        _window.FireteamsContainer.AddChild(unassignedContainer);
    }
}
