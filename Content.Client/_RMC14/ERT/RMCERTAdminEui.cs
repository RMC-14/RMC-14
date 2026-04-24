using System.Linq;
using Content.Client.Eui;
using Content.Shared._RMC14.ERT;
using Content.Shared.Eui;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client._RMC14.ERT;

[UsedImplicitly]
/// <summary>
/// Client-side admin EUI that renders requests as a lightweight action list.
/// </summary>
public sealed class RMCERTAdminEui : BaseEui
{
    private RMCERTAdminWindow? _window;

    public override void Opened()
    {
        _window = new RMCERTAdminWindow();
        _window.OpenCentered();
    }

    public override void HandleState(EuiStateBase state)
    {
        if (_window == null || state is not RMCERTAdminEuiState s)
            return;

        _window.Requests.DisposeAllChildren();

        if (s.Requests.Count == 0)
        {
            _window.Requests.AddChild(new Label { Text = Loc.GetString("rmc-ert-admin-no-requests") });
            return;
        }

        // Rebuild the request list from server state each update; the UI is small enough that a diff layer is unnecessary.
        foreach (var request in s.Requests)
        {
            var box = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                HorizontalExpand = true,
                Margin = new Thickness(0, 0, 0, 8),
            };

            box.AddChild(new Label
            {
                Text = Loc.GetString("rmc-ert-admin-row-summary",
                    ("state", RMCERTLoc.GetState(request.State)),
                    ("source", RMCERTLoc.GetSource(request.Source)),
                    ("requester", request.RequesterName),
                    ("sourceName", request.SourceName),
                    ("createdAt", request.CreatedAt)),
            });

            if (!string.IsNullOrWhiteSpace(request.Reason))
            {
                box.AddChild(new Label
                {
                    Text = Loc.GetString("rmc-ert-admin-row-reason", ("reason", request.Reason)),
                });
            }

            if (!string.IsNullOrWhiteSpace(request.SelectedCall))
            {
                box.AddChild(new Label
                {
                    Text = Loc.GetString("rmc-ert-admin-row-selected", ("call", request.SelectedCall)),
                });
            }

            if (!string.IsNullOrWhiteSpace(request.LastError))
            {
                box.AddChild(new Label
                {
                    Text = Loc.GetString("rmc-ert-admin-row-error", ("error", request.LastError)),
                    FontColorOverride = Color.OrangeRed,
                });
            }

            var actions = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Horizontal,
                HorizontalExpand = true,
            };

            if (request.State == RMCERTRequestState.PendingAdmin)
            {
                AddButton(actions, Loc.GetString("rmc-ert-admin-action-approve-random"), _ => SendMessage(new RMCERTAdminApproveRandomMsg(request.Id)));
                AddButton(actions, Loc.GetString("rmc-ert-admin-action-deny"), _ => SendMessage(new RMCERTAdminDenyMsg(request.Id)));

                var allowed = request.AllowedCalls.Count == 0
                    ? s.Calls.Where(c => c.AdminSelectable)
                    : s.Calls.Where(c => request.AllowedCalls.Contains(c.Id));

                foreach (var call in allowed)
                {
                    var label = string.IsNullOrWhiteSpace(call.AdminButtonLabel)
                        ? Loc.GetString("rmc-ert-admin-action-send", ("call", call.Name))
                        : call.AdminButtonLabel!;
                    AddButton(actions, label, _ => SendMessage(new RMCERTAdminApproveSpecificMsg(request.Id, call.Id)));
                }
            }
            else if (request.State == RMCERTRequestState.Recruiting)
            {
                AddButton(actions, Loc.GetString("rmc-ert-admin-action-launch"), _ => SendMessage(new RMCERTAdminLaunchMsg(request.Id)));
                AddButton(actions, Loc.GetString("rmc-ert-admin-action-cancel"), _ => SendMessage(new RMCERTAdminCancelMsg(request.Id)));
            }
            else if (request.State is RMCERTRequestState.PendingDispatch or RMCERTRequestState.Spawning or RMCERTRequestState.Launching)
            {
                AddButton(actions, Loc.GetString("rmc-ert-admin-action-cancel"), _ => SendMessage(new RMCERTAdminCancelMsg(request.Id)));
            }
            else if (request.State == RMCERTRequestState.Arrived)
            {
                AddButton(actions, Loc.GetString("rmc-ert-admin-action-complete"), _ => SendMessage(new RMCERTAdminCompleteMsg(request.Id)));
            }

            box.AddChild(actions);
            _window.Requests.AddChild(box);
        }
    }

    public override void Closed()
    {
        _window?.Close();
        _window = null;
    }

    private static void AddButton(BoxContainer parent, string text, Action<ButtonEventArgs> onPressed)
    {
        var button = new Button
        {
            Text = text,
            MinWidth = 110,
            Margin = new Thickness(0, 0, 4, 0),
            StyleClasses = { "OpenBoth" },
        };
        button.OnPressed += onPressed;
        parent.AddChild(button);
    }
}
