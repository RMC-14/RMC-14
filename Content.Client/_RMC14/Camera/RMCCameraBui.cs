using System.Runtime.InteropServices;
using Content.Client._RMC14.UserInterface;
using Content.Client.Eye;
using Content.Client.Message;
using Content.Client.UserInterface.ControlExtensions;
using Content.Shared._RMC14.Camera;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._RMC14.Camera;

public sealed class RMCCameraBui : RMCPopOutBui<RMCCameraWindow>
{
    private EntityUid? _currentCamera;
    private Button? _currentCameraButton;

    private readonly EyeLerpingSystem _eyeLerping;
    private readonly RMCCameraSystem _system;

    protected override RMCCameraWindow? Window { get; set; }

    public RMCCameraBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _eyeLerping = EntMan.System<EyeLerpingSystem>();
        _system = EntMan.System<RMCCameraSystem>();
    }

    protected override void Open()
    {
        base.Open();
        Window = this.CreatePopOutableWindow<RMCCameraWindow>();
        Window.SearchBar.OnTextChanged += _ => RefreshSearch();
        Window.PreviousCameraButton.Text = "<";
        Window.NextCameraButton.Text = ">";
        Window.PreviousCameraButton.OnPressed += _ => SendPredictedMessage(new RMCCameraPreviousBuiMsg());
        Window.NextCameraButton.OnPressed += _ => SendPredictedMessage(new RMCCameraNextBuiMsg());

        Refresh();
    }

    public void Refresh()
    {
        if (Window == null)
            return;

        if (!EntMan.TryGetComponent(Owner, out RMCCameraComputerComponent? computer))
            return;

        if (computer.Title is { } title)
            Window.Title = Loc.GetString(title);

        var currentNetCamera = EntMan.GetNetEntity(computer.CurrentCamera);
        var ids = CollectionsMarshal.AsSpan(computer.CameraIds);
        var names = CollectionsMarshal.AsSpan(computer.CameraNames);
        for (var i = 0; i < ids.Length; i++)
        {
            if (i >= names.Length)
                continue;

            RMCCameraButton button;
            if (i < Window.CamerasContainer.ChildCount)
            {
                if (Window.CamerasContainer.GetChild(i) is not RMCCameraButton child)
                    continue;

                button = child;
            }
            else
            {
                button = new RMCCameraButton();
                Window.CamerasContainer.AddChild(button);
            }

            var id = ids[i];
            var name = names[i];
            button.Label.SetMarkupPermissive($"[font size=11][color=white]{name}[/color][/font]");
            button.Pressed = id == currentNetCamera;
            button.OnPressed += _ =>
            {
                if (_currentCameraButton != null)
                    _currentCameraButton.Pressed = false;

                _currentCameraButton = button;
                SendPredictedMessage(new RMCCameraWatchBuiMsg(id));
            };
        }

        for (var i = Window.CamerasContainer.ChildCount - 1; i >= ids.Length; i--)
        {
            Window.CamerasContainer.RemoveChild(i);
        }

        RefreshSearch();
        RefreshCamera();
    }

    private void RefreshSearch()
    {
        if (Window == null)
            return;

        foreach (var control in Window.CamerasContainer.Children)
        {
            if (control is not Button button)
                continue;

            button.Visible = button.ChildrenContainText(Window.SearchBar.Text);
        }
    }

    private void RefreshCamera()
    {
        if (Window == null)
            return;

        if (!EntMan.TryGetComponent(Owner, out RMCCameraComputerComponent? computer))
            return;

        if (_currentCamera is { } oldCamera)
            _eyeLerping.RemoveEye(oldCamera);

        if (computer.CurrentCamera is not { } camera)
            return;

        _eyeLerping.AddEye(camera);
        _currentCamera = camera;

        if (EntMan.TryGetComponent(camera, out EyeComponent? eye))
            Window.Viewport.Eye = eye.Eye;

        if (_system.GetComputerCameraName((Owner, computer), camera, out var name))
            Window.CameraName.Text = name;
    }
}
