using System.Linq;
using Content.Client.UserInterface.Controls;
using Content.Client.Eui;
using Content.Shared._RMC14.ERT;
using Content.Shared.Eui;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client._RMC14.ERT;

/// <summary>
/// Client-side admin EUI that renders requests as a lightweight action list.
/// </summary>
[UsedImplicitly]
public sealed class RMCERTAdminEui : BaseEui
{
    private static readonly Color RequestCardBackground = Color.FromHex("#2b2c38");
    private static readonly Color RequestCardBorder = Color.FromHex("#454966");
    private static readonly Color RequestMutedColor = Color.FromHex("#9a9aa2");
    private static readonly Color RequestLabelColor = Color.FromHex("#78c9ff");

    private RMCERTAdminWindow? _window;
    private bool _forceTabAttached = true;

    public override void Opened()
    {
        _window = new RMCERTAdminWindow();
        _forceTabAttached = true;
        _window.OpenCentered();
    }

    public override void HandleState(EuiStateBase state)
    {
        if (_window == null || state is not RMCERTAdminEuiState s)
            return;

        SetForceTabVisible(s.CanForceCalls);
        RenderRequests(s);
        RenderForceCalls(s);
    }

    private void RenderRequests(RMCERTAdminEuiState s)
    {
        if (_window == null)
            return;

        _window.Requests.DisposeAllChildren();

        if (s.Requests.Count == 0)
        {
            _window.Requests.AddChild(new Label { Text = Loc.GetString("rmc-ert-admin-no-requests") });
            return;
        }

        foreach (var request in s.Requests)
            _window.Requests.AddChild(BuildRequestCard(request, s));
    }

    private Control BuildRequestCard(RMCERTRequestOption request, RMCERTAdminEuiState state)
    {
        var stateColor = GetRequestStateColor(request.State);
        var panel = new PanelContainer
        {
            HorizontalExpand = true,
            Margin = new Thickness(0, 0, 0, 6),
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = RequestCardBackground,
                BorderColor = stateColor,
                BorderThickness = new Thickness(2, 1, 1, 1),
            },
        };

        var body = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            Margin = new Thickness(8, 6, 8, 6),
            SeparationOverride = 4,
        };

        var header = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            SeparationOverride = 8,
        };
        header.AddChild(CreateStatusBadge(RMCERTLoc.GetState(request.State), stateColor));
        header.AddChild(CreateMutedLabel(Loc.GetString("rmc-ert-admin-card-created", ("time", request.CreatedAt))));
        body.AddChild(header);

        AddRequestInfoRow(body, Loc.GetString("rmc-ert-admin-card-requester"), request.RequesterName);
        AddRequestInfoRow(body, Loc.GetString("rmc-ert-admin-card-source"), RMCERTLoc.GetSource(request.Source));
        AddRequestInfoRow(body, Loc.GetString("rmc-ert-admin-card-via"), request.SourceName);
        AddRequestInfoRow(body,
            Loc.GetString("rmc-ert-admin-card-selected"),
            string.IsNullOrWhiteSpace(request.SelectedCall)
                ? Loc.GetString("rmc-ert-admin-card-none")
                : request.SelectedCall);
        AddRequestInfoRow(body,
            Loc.GetString("rmc-ert-admin-card-reason"),
            string.IsNullOrWhiteSpace(request.Reason)
                ? Loc.GetString("rmc-ert-admin-card-none")
                : request.Reason);

        if (!string.IsNullOrWhiteSpace(request.LastError))
            AddRequestInfoRow(body, Loc.GetString("rmc-ert-admin-card-error"), request.LastError, Color.OrangeRed);

        if (!string.IsNullOrWhiteSpace(request.LastWarning))
            AddRequestInfoRow(body, Loc.GetString("rmc-ert-admin-card-warning"), request.LastWarning, Color.Orange);

        var actions = new GridContainer
        {
            Columns = 4,
            HorizontalExpand = true,
            HSeparationOverride = 4,
            VSeparationOverride = 4,
        };
        var actionCount = PopulateRequestActions(actions, request, state);

        body.AddChild(new Label
        {
            Text = Loc.GetString("rmc-ert-admin-card-actions"),
            FontColorOverride = RequestMutedColor,
            Margin = new Thickness(0, 2, 0, 0),
        });

        if (actionCount > 0)
            body.AddChild(actions);
        else
            body.AddChild(CreateMutedLabel(Loc.GetString("rmc-ert-admin-card-no-actions")));

        panel.AddChild(body);
        return panel;
    }

    private int PopulateRequestActions(GridContainer actions, RMCERTRequestOption request, RMCERTAdminEuiState state)
    {
        var count = 0;

        if (request.State == RMCERTRequestState.PendingAdmin)
        {
            if (request.AllowRandomSelection)
            {
                AddRequestActionButton(actions, Loc.GetString("rmc-ert-admin-action-approve-random"), _ => SendMessage(new RMCERTAdminApproveRandomMsg(request.Id)));
                count++;
            }

            AddRequestActionButton(actions, Loc.GetString("rmc-ert-admin-action-deny"), _ => SendMessage(new RMCERTAdminDenyMsg(request.Id)));
            count++;

            if (request.AllowSpecificSelection)
            {
                var allowed = request.AllowedCalls.Count == 0
                    ? state.Calls.Where(c => c.AdminSelectable)
                    : state.Calls.Where(c => request.AllowedCalls.Contains(c.Id));

                foreach (var call in allowed)
                {
                    var callId = call.Id;
                    var label = string.IsNullOrWhiteSpace(call.AdminButtonLabel)
                        ? Loc.GetString("rmc-ert-admin-action-send", ("call", call.Name))
                        : call.AdminButtonLabel!;
                    AddRequestActionButton(actions, label, _ => SendMessage(new RMCERTAdminApproveSpecificMsg(request.Id, callId)));
                    count++;
                }
            }
        }
        else if (request.State == RMCERTRequestState.Recruiting)
        {
            AddRequestActionButton(actions, Loc.GetString("rmc-ert-admin-action-launch"), _ => SendMessage(new RMCERTAdminLaunchMsg(request.Id)));
            AddRequestActionButton(actions, Loc.GetString("rmc-ert-admin-action-cancel"), _ => SendMessage(new RMCERTAdminCancelMsg(request.Id)));
            count += 2;
        }
        else if (request.State is RMCERTRequestState.PendingDispatch or RMCERTRequestState.Spawning or RMCERTRequestState.Launching)
        {
            AddRequestActionButton(actions, Loc.GetString("rmc-ert-admin-action-cancel"), _ => SendMessage(new RMCERTAdminCancelMsg(request.Id)));
            count++;
        }

        return count;
    }

    private static Control CreateStatusBadge(string text, Color color)
    {
        var badge = new PanelContainer
        {
            MinWidth = 154,
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = Color.FromHex("#20222d"),
                BorderColor = color,
                BorderThickness = new Thickness(1),
            },
        };
        badge.AddChild(new Label
        {
            Text = text,
            Align = Label.AlignMode.Center,
            FontColorOverride = color,
            HorizontalExpand = true,
            Margin = new Thickness(4, 1, 4, 1),
        });
        return badge;
    }

    private static void AddRequestInfoRow(BoxContainer parent, string label, string value, Color? valueColor = null)
    {
        var row = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            SeparationOverride = 8,
        };
        row.AddChild(new Label
        {
            Text = label,
            FontColorOverride = RequestLabelColor,
            SetWidth = 92,
            ClipText = true,
        });
        row.AddChild(new Label
        {
            Text = string.IsNullOrWhiteSpace(value) ? "-" : value,
            FontColorOverride = valueColor ?? Color.White,
            HorizontalExpand = true,
        });
        parent.AddChild(row);
    }

    private static Label CreateMutedLabel(string text)
    {
        return new Label
        {
            Text = text,
            FontColorOverride = RequestMutedColor,
            ClipText = true,
            HorizontalExpand = true,
        };
    }

    private static Color GetRequestStateColor(RMCERTRequestState state)
    {
        return state switch
        {
            RMCERTRequestState.PendingAdmin => Color.FromHex("#7fd0ff"),
            RMCERTRequestState.PendingDispatch => Color.FromHex("#d7c46a"),
            RMCERTRequestState.Recruiting => Color.FromHex("#d7c46a"),
            RMCERTRequestState.Spawning => Color.FromHex("#8bd17c"),
            RMCERTRequestState.Launching => Color.FromHex("#8bd17c"),
            RMCERTRequestState.Arrived => Color.FromHex("#8bd17c"),
            RMCERTRequestState.Completed => Color.FromHex("#9a9aa2"),
            RMCERTRequestState.Denied => Color.FromHex("#9a9aa2"),
            RMCERTRequestState.Cancelled => Color.FromHex("#9a9aa2"),
            RMCERTRequestState.Failed => Color.OrangeRed,
            _ => RequestCardBorder,
        };
    }

    private void RenderForceCalls(RMCERTAdminEuiState s)
    {
        if (_window == null || !s.CanForceCalls)
            return;

        _window.ForceCalls.DisposeAllChildren();

        if (s.ForceCalls.Count == 0)
        {
            _window.ForceCalls.AddChild(new Label { Text = Loc.GetString("rmc-ert-admin-force-no-calls") });
            return;
        }

        foreach (var group in s.ForceCalls.GroupBy(c => c.Category))
        {
            _window.ForceCalls.AddChild(new Label
            {
                Text = Loc.GetString("rmc-ert-admin-force-row-category", ("category", group.Key)),
                FontColorOverride = Color.LightSkyBlue,
                Margin = new Thickness(0, 4, 0, 2),
            });

            foreach (var call in group)
            {
                var panel = new PanelContainer
                {
                    HorizontalExpand = true,
                    Margin = new Thickness(0, 0, 0, 3),
                };

                var row = new BoxContainer
                {
                    Orientation = BoxContainer.LayoutOrientation.Horizontal,
                    HorizontalExpand = true,
                    Margin = new Thickness(6, 3, 6, 3),
                };

                var details = new BoxContainer
                {
                    Orientation = BoxContainer.LayoutOrientation.Vertical,
                    HorizontalExpand = true,
                };

                details.AddChild(new Label
                {
                    Text = call.Name,
                    HorizontalExpand = true,
                });

                var meta = string.IsNullOrWhiteSpace(call.Organization)
                    ? Loc.GetString("rmc-ert-admin-force-row-id", ("id", call.Id))
                    : Loc.GetString("rmc-ert-admin-force-row-compact-meta",
                        ("id", call.Id),
                        ("organization", call.Organization));
                details.AddChild(new Label
                {
                    Text = meta,
                    FontColorOverride = Color.Gray,
                    HorizontalExpand = true,
                });

                row.AddChild(details);

                var callId = call.Id;
                AddForceButton(row, Loc.GetString("rmc-ert-admin-action-force-call-short"), _ =>
                {
                    var reason = _window?.ForceReason.Text ?? string.Empty;
                    SendMessage(new RMCERTAdminForceCallMsg(callId, reason));
                }, false);

                panel.AddChild(row);
                _window.ForceCalls.AddChild(panel);
            }
        }
    }

    private void SetForceTabVisible(bool visible)
    {
        if (_window == null)
            return;

        if (visible)
        {
            if (_forceTabAttached)
                return;

            _window.Tabs.AddChild(_window.ForceTab);
            _window.Tabs.SetTabTitle(1, Loc.GetString("rmc-ert-admin-tab-force"));
            _forceTabAttached = true;
            return;
        }

        if (!_forceTabAttached)
            return;

        if (_window.Tabs.CurrentTab == 1)
            _window.Tabs.CurrentTab = 0;

        _window.Tabs.RemoveChild(_window.ForceTab);
        _forceTabAttached = false;
    }

    public override void Closed()
    {
        _window?.Close();
        _window = null;
    }

    private static Button AddRequestActionButton(GridContainer parent, string text, Action<ButtonEventArgs> onPressed, bool disabled = false)
    {
        var button = new ConfirmButton
        {
            Text = text,
            ConfirmationText = Loc.GetString("generic-confirm"),
            MinWidth = 118,
            StyleClasses = { "OpenBoth" },
            Disabled = disabled,
        };
        button.OnPressed += onPressed;
        parent.AddChild(button);
        return button;
    }

    private static Button AddForceButton(BoxContainer parent, string text, Action<ButtonEventArgs> onPressed, bool disabled)
    {
        var button = new ConfirmButton
        {
            Text = text,
            ConfirmationText = Loc.GetString("generic-confirm"),
            MinWidth = 68,
            Margin = new Thickness(6, 0, 0, 0),
            VerticalAlignment = Control.VAlignment.Center,
            Disabled = disabled,
        };
        button.OnPressed += onPressed;
        parent.AddChild(button);
        return button;
    }
}
