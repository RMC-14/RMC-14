using System.Runtime.InteropServices;
using Content.Client.Eye;
using Content.Shared._RMC14.Camera;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._RMC14.Camera;

public sealed class RMCCameraBui : BoundUserInterface
{
    [ViewVariables]
    private RMCCameraWindow? _window;

    private EntityUid? _currentCamera;
    private Button? _currentCameraButton;

    private readonly EyeLerpingSystem _eyeLerping;
    private readonly RMCCameraSystem _system;

    public RMCCameraBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _eyeLerping = EntMan.System<EyeLerpingSystem>();
        _system = EntMan.System<RMCCameraSystem>();
    }

    protected override void Open()
    {
        _window = new RMCCameraWindow();
        _window.OnClose += Close;
        _window.SearchBar.OnTextChanged += _ => RefreshSearch();
        _window.PreviousCameraButton.Text = "<";
        _window.NextCameraButton.Text = ">";
        _window.PreviousCameraButton.OnPressed += _ => SendPredictedMessage(new RMCCameraPreviousBuiMsg());
        _window.NextCameraButton.OnPressed += _ => SendPredictedMessage(new RMCCameraNextBuiMsg());

        Refresh();

        _window.OpenCentered();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            _window?.Dispose();
    }

    public void Refresh()
    {
        if (_window == null)
            return;

        if (!EntMan.TryGetComponent(Owner, out RMCCameraComputerComponent? computer))
            return;

        _window.CamerasContainer.DisposeAllChildren();
        var currentNetCamera = EntMan.GetNetEntity(computer.CurrentCamera);
        var ids = CollectionsMarshal.AsSpan(computer.CameraIds);
        var names = CollectionsMarshal.AsSpan(computer.CameraNames);
        for (var i = 0; i < ids.Length; i++)
        {
            if (i >= names.Length)
                continue;

            var id = ids[i];
            var name = names[i];
            var button = new Button
            {
                Text = name,
                StyleClasses = { "OpenBoth" },
                ToggleMode = true,
            };

            button.Pressed = id == currentNetCamera;
            button.OnPressed += _ =>
            {
                if (_currentCameraButton != null)
                    _currentCameraButton.Pressed = false;

                _currentCameraButton = button;
                SendPredictedMessage(new RMCCameraWatchBuiMsg(id));
            };

            _window.CamerasContainer.AddChild(button);
        }

        RefreshSearch();
        RefreshCamera();
    }

    private void RefreshSearch()
    {
        if (_window == null)
            return;

        foreach (var control in _window.CamerasContainer.Children)
        {
            if (control is not Button button)
                continue;

            button.Visible = button.Text == null ||
                             button.Text.Contains(_window.SearchBar.Text, StringComparison.OrdinalIgnoreCase);
        }
    }

    private void RefreshCamera()
    {
        if (_window is not { Disposed: false })
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
            _window.Viewport.Eye = eye.Eye;

        if (_system.GetComputerCameraName((Owner, computer), camera, out var name))
            _window.CameraName.Text = name;
    }
}
