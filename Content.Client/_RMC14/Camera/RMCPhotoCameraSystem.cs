using System.IO;
using System.Numerics;
using Content.Shared._RMC14.Camera.PhotoCamera;
using Content.Shared._RMC14.Weapons.Common;
using Content.Shared.Coordinates.Helpers;
using Robust.Client.Graphics;
using Robust.Client.Input;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Content.Client._RMC14.Camera;

public sealed class RMCPhotoCameraSystem : SharedRmcPhotoCameraSystem
{
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;

    private (IClydeViewport viewport, EntityUid eye, NetEntity user)? _pendingCapture;
    private bool _skipFrame;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<TakePhotoEvent>(OnTakePhoto);

        SubscribeLocalEvent<RMCPhotoCameraComponent, UniqueActionEvent>(OnUniqueAction);
    }

    private void OnUniqueAction(Entity<RMCPhotoCameraComponent> ent, ref UniqueActionEvent args)
    {
        if (!Timing.IsFirstTimePredicted || args.Handled)
            return;

        if (ent.Comp.PhotoPrintedAt != null || ent.Comp.ImageData != null)
            return;

        var world = _eye.PixelToMap(_inputManager.MouseScreenPosition);
        var center = TransformSystem.ToCoordinates(world).SnapToGrid();

        if (!center.IsValid(EntityManager))
            return;

        RaiseNetworkEvent(new RequestPhotoCaptureEvent(GetNetCoordinates(center)));
    }

    private void OnTakePhoto(TakePhotoEvent ev, EntitySessionEventArgs args)
    {
        if (!TryComp(GetEntity(ev.Camera), out RMCPhotoCameraComponent? cameraComp))
            return;

        TakePhoto(cameraComp.Resolution, ev.Eye, ev.User, ev.Zoom);
    }

    private void TakePhoto(int resolution, NetEntity netEye, NetEntity user, Vector2 zoom)
    {
        var eye = GetEntity(netEye);
        if (!TryComp(eye, out EyeComponent? eyeComp))
            return;

        EyeSystem.SetZoom(eye, zoom);

        var size = new Vector2i(resolution, resolution);
        var viewport = _clyde.CreateViewport(size);
        viewport.AutomaticRender = true;
        viewport.Eye = eyeComp.Eye;

        _pendingCapture = (viewport, eye, user);
    }

    public OwnedTexture GetPhoto(byte[] imageData)
    {
        using var ms = new MemoryStream(imageData);
        return _clyde.LoadTextureFromPNGStream(ms);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (_pendingCapture is not { } pending)
            return;

        if (!_skipFrame)
        {
            _skipFrame = true;
            return;
        }

        _skipFrame = false;
        _pendingCapture = null;

        var (viewport, eye, user) = pending;

        viewport.RenderTarget.CopyPixelsToMemory<Rgba32>(image =>
        {
            using var output = new MemoryStream();
            image.SaveAsPng(output);

            RaiseNetworkEvent(new PhotoCaptureEvent(output.ToArray(), GetNetEntity(eye), user));
        });
    }
}
