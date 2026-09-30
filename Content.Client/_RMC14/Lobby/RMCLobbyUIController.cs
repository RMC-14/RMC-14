using System.Numerics;
using Content.Client.Lobby;
using Content.Client.Players.PlayTimeTracking;
using Content.Shared._RMC14.FaxResponder;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.JoinXeno;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.StatusIcon;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Lobby;

public sealed class RMCLobbyUIController : UIController
{
    [Dependency] private readonly JobRequirementsManager _jobRequirements = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IClientPreferencesManager _preferencesManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    private JoinXenoWindow? _joinXenoWindow;
    private JoinFaxResponderWindow? _joinFaxResponderWindow;

    private ContainerButton? _weyaButton;
    private ContainerButton? _freePressButton;

    private int _weyaSlots;
    private int _freePressSlots;

    public override void Initialize()
    {
        SubscribeLocalEvent<BurrowedLarvaChangedEvent>(OnBurrowedLarvaChanged);
        SubscribeLocalEvent<FaxResponderStatusChangedEvent>(OnFaxResponderStatusChanged);
        _net.RegisterNetMessage<FaxResponderStatusEvent>(OnFaxResponderStatus);
    }

    private void OnBurrowedLarvaChanged(ref BurrowedLarvaChangedEvent ev)
    {
        if (_joinXenoWindow is not { IsOpen: true })
            return;

        RefreshXenoWindow(ev.Larva);
    }

    private void OnFaxResponderStatus(FaxResponderStatusEvent ev)
    {
        _weyaSlots = ev.WeyaSlots;
        _freePressSlots = ev.FreePressSlots;

        var changedEv = new FaxResponderStatusChangedEvent(_weyaSlots, _freePressSlots);
        EntityManager.EventBus.RaiseEvent(EventSource.Local, ref changedEv);
    }

    private void OnFaxResponderStatusChanged(ref FaxResponderStatusChangedEvent ev)
    {
        if (_joinFaxResponderWindow is not { IsOpen: true })
            return;

        RefreshFaxResponderWindow(ev.WeyaSlots, ev.FreePressSlots);
    }

    public void OpenJoinXenoWindow()
    {
        var system = EntityManager.System<JoinXenoSystem>();
        RefreshXenoWindow(system.ClientBurrowedLarva);
    }

    public void OpenJoinFaxResponderWindow()
    {
        RefreshFaxResponderWindow(_weyaSlots, _freePressSlots);
        RequestFaxResponderStatus();
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

    private void RefreshFaxResponderWindow(int weyaSlots, int freePressSlots)
    {
        if (_joinFaxResponderWindow == null || _joinFaxResponderWindow.Disposed)
        {
            _joinFaxResponderWindow = new JoinFaxResponderWindow();
            _joinFaxResponderWindow.OnClose += () =>
            {
                _joinFaxResponderWindow = null;
                _weyaButton = null;
                _freePressButton = null;
            };

            var sprites = EntityManager.System<SpriteSystem>();

            _weyaButton = CreateJobButton(sprites, "CMWeYaresponder", "CMJobIconWeYa", Loc.GetString("rmc-lobby-join-weya"), weyaSlots);
            _weyaButton.OnPressed += _ =>
            {
                ClientJoinFaxResponder("CMWeYaresponder");
                _joinFaxResponderWindow.Close();
            };
            _joinFaxResponderWindow.JobList.AddChild(_weyaButton);

            _freePressButton = CreateJobButton(sprites, "CMFreePressResponder", "CMJobIconReporter", Loc.GetString("rmc-lobby-join-free-press"), freePressSlots);
            _freePressButton.OnPressed += _ =>
            {
                ClientJoinFaxResponder("CMFreePressResponder");
                _joinFaxResponderWindow.Close();
            };
            _joinFaxResponderWindow.JobList.AddChild(_freePressButton);

            _joinFaxResponderWindow.OpenCentered();
        }

        if (_weyaButton != null)
            _weyaButton.Disabled = weyaSlots <= 0;

        if (_freePressButton != null)
            _freePressButton.Disabled = freePressSlots <= 0;
    }

    private ContainerButton CreateJobButton(SpriteSystem sprites, string jobId, string iconId, string name, int? slots)
    {
        var button = new ContainerButton();
        button.AddStyleClass(ContainerButton.StyleClassButton);

        var container = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true
        };

        if (_prototypes.TryIndex<JobIconPrototype>(iconId, out var jobIcon))
        {
            var icon = new TextureRect
            {
                TextureScale = new Vector2(2, 2),
                VerticalAlignment = Control.VAlignment.Center,
                Texture = sprites.Frame0(jobIcon.Icon)
            };
            container.AddChild(icon);
        }

        var slotText = slots != null
            ? Loc.GetString("late-join-gui-job-slot-capped", ("jobName", name), ("amount", slots))
            : Loc.GetString("late-join-gui-job-slot-uncapped", ("jobName", name));

        var label = new Label
        {
            Text = slotText,
            Margin = new Thickness(5f, 0, 0, 0)
        };
        container.AddChild(label);

        // Check whitelist
        if (_prototypes.TryIndex<JobPrototype>(jobId, out var jobPrototype) &&
            !_jobRequirements.IsAllowed(jobPrototype, (HumanoidCharacterProfile?)_preferencesManager.Preferences?.SelectedCharacter, out var reason))
        {
            button.Disabled = true;

            if (!reason.IsEmpty)
            {
                var tooltip = new Tooltip();
                tooltip.SetMessage(reason);
                button.TooltipSupplier = _ => tooltip;
            }

            container.AddChild(new TextureRect
            {
                TextureScale = new Vector2(0.4f, 0.4f),
                Stretch = TextureRect.StretchMode.KeepCentered,
                Texture = sprites.Frame0(new SpriteSpecifier.Texture(new ("/Textures/Interface/Nano/lock.svg.192dpi.png"))),
                HorizontalExpand = true,
                HorizontalAlignment = Control.HAlignment.Right,
            });
        }

        button.AddChild(container);
        return button;
    }

    private void RequestFaxResponderStatus()
    {
        var ev = new FaxResponderStatusRequest();
        EntityManager.EntityNetManager?.SendSystemNetworkMessage(ev);
    }

    private void ClientJoinFaxResponder(string jobId)
    {
        var ev = new JoinFaxResponderRequest(jobId);
        EntityManager.EntityNetManager?.SendSystemNetworkMessage(ev);
    }
}
