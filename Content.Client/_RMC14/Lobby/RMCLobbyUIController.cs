using Content.Client._RMC14.FaxResponder;
using Content.Client.Lobby;
using Content.Client.Players.PlayTimeTracking;
using Content.Shared._RMC14.FaxResponder;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.JoinXeno;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Lobby;

public sealed class RMCLobbyUIController : UIController
{
    [Dependency] private readonly JobRequirementsManager _jobRequirements = default!;
    [Dependency] private readonly IClientPreferencesManager _preferences = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    private JoinXenoWindow? _joinXenoWindow;
    private JoinFaxResponderWindow? _joinFaxResponderWindow;

    public override void Initialize()
    {
        SubscribeLocalEvent<BurrowedLarvaChangedEvent>(OnBurrowedLarvaChanged);
        SubscribeLocalEvent<FaxResponderStatusChangedEvent>(OnFaxResponderStatusChanged);
        _jobRequirements.Updated += OnJobRequirementsUpdated;
    }

    // Xeno

    private void OnBurrowedLarvaChanged(ref BurrowedLarvaChangedEvent ev)
    {
        if (_joinXenoWindow is not { IsOpen: true })
            return;

        RefreshXenoWindow(ev.Larva);
    }

    public void OpenJoinXenoWindow()
    {
        var system = EntityManager.System<JoinXenoSystem>();
        RefreshXenoWindow(system.ClientBurrowedLarva);
    }

    private void RefreshXenoWindow(int larva)
    {
        var system = EntityManager.System<JoinXenoSystem>();
        if (_joinXenoWindow == null || _joinXenoWindow.Disposed)
        {
            _joinXenoWindow = new JoinXenoWindow();
            _joinXenoWindow.OnClose += () => _joinXenoWindow = null;
            _joinXenoWindow.LarvaButton.OnPressed += _ =>
            {
                system.ClientJoinLarva();
                _joinXenoWindow.Close();
            };

            _joinXenoWindow.OpenCentered();
        }

        if (larva == 0)
        {
            _joinXenoWindow.Label.Text = Loc.GetString("rmc-lobby-no-burrowed-larva");
            _joinXenoWindow.Buttons.Visible = false;
        }
        else
        {
            _joinXenoWindow.Label.Text = Loc.GetString("rmc-lobby-burrowed-larva-available");
            _joinXenoWindow.Buttons.Visible = true;
        }

        system.RequestBurrowedLarvaStatus();
    }

    // Fax Responder

    public void OpenJoinFaxResponderWindow()
    {
        if (_joinFaxResponderWindow == null || _joinFaxResponderWindow.Disposed)
        {
            _joinFaxResponderWindow = new JoinFaxResponderWindow();
            _joinFaxResponderWindow.OnClose += () => _joinFaxResponderWindow = null;
            _joinFaxResponderWindow.OpenCentered();
        }

        // Show the last known status straight away, then ask the server for a fresh one
        var system = EntityManager.System<FaxResponderSystem>();
        RefreshFaxResponderWindow(system.Jobs);
        system.RequestStatus();
    }

    private void OnFaxResponderStatusChanged(FaxResponderStatusChangedEvent ev)
    {
        RefreshFaxResponderWindow(ev.Jobs);
    }

    private void OnJobRequirementsUpdated()
    {
        if (_joinFaxResponderWindow == null || _joinFaxResponderWindow.Disposed)
            return;

        RefreshFaxResponderWindow(EntityManager.System<FaxResponderSystem>().Jobs);
    }

    private void RefreshFaxResponderWindow(List<FaxResponderJobStatus> jobs)
    {
        if (_joinFaxResponderWindow == null || _joinFaxResponderWindow.Disposed)
            return;

        _joinFaxResponderWindow.Buttons.RemoveAllChildren();

        if (jobs.Count == 0)
        {
            _joinFaxResponderWindow.Label.Text = Loc.GetString("rmc-lobby-fax-responder-unavailable");
            return;
        }

        _joinFaxResponderWindow.Label.Text = Loc.GetString("rmc-lobby-fax-responder-available");

        var profile = _preferences.Preferences?.SelectedCharacter as HumanoidCharacterProfile;
        foreach (var status in jobs)
        {
            if (!_prototypes.TryIndex(status.Job, out JobPrototype? job))
                continue;

            var button = new Button
            {
                Text = Loc.GetString(job.Name),
                HorizontalExpand = true,
                MinHeight = 35,
            };

            // Grey out the same way the late join menu does
            FormattedMessage? reason = null;
            if (status.Taken)
                reason = FormattedMessage.FromUnformatted(Loc.GetString("rmc-lobby-fax-responder-taken"));
            else
                _jobRequirements.IsAllowed(job, profile, out reason);

            if (reason != null)
            {
                button.Disabled = true;
                var tooltip = new Tooltip();
                tooltip.SetMessage(reason);
                button.TooltipSupplier = _ => tooltip;
            }

            var jobId = status.Job;
            button.OnPressed += _ =>
            {
                EntityManager.System<FaxResponderSystem>().RequestJoin(jobId);
                _joinFaxResponderWindow?.Close();
            };

            _joinFaxResponderWindow.Buttons.AddChild(button);
        }
    }
}
