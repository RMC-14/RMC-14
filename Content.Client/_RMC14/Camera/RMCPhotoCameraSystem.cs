using System.IO;
using System.Numerics;
using Content.Client.Eye;
using Content.Shared._RMC14.Camera;
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
    [Dependency] private readonly SharedEyeSystem _eyeSystem = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly EyeLerpingSystem _eyeLerping = default!;

    private (IClydeViewport viewport, EntityUid eye)? _pendingCapture;
    private bool _skipFrame;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCPhotoCameraComponent, UniqueActionEvent>(OnUniqueAction);
    }

    private void OnUniqueAction(Entity<RMCPhotoCameraComponent> ent, ref UniqueActionEvent args)
    {
        if (!Timing.IsFirstTimePredicted || args.Handled)
            return;

        if (ent.Comp.PhotoPrintedAt != null || ent.Comp.ImageData != null)
            return;

        TakePhoto(ent.Comp.ZoomLevel, ent.Comp.Resolution);
    }

    private void TakePhoto(float zoomLevel, int resolution)
    {
        var world = _eye.PixelToMap(_inputManager.MouseScreenPosition);
        var center = TransformSystem.ToCoordinates(world).SnapToGrid();

        if (!center.IsValid(EntityManager))
            return;

        var eye = Spawn(null, center);

        var eyeComp = EnsureComp<EyeComponent>(eye);
        EnsureComp<RMCStaticZoomLevelComponent>(eye);

        _eyeSystem.SetZoom(eye, new Vector2(zoomLevel, zoomLevel), eyeComp);
        _eyeSystem.SetDrawFov(eye, true, eyeComp);
        _eyeSystem.UpdateEye((eye, eyeComp));
        _eyeSystem.SetDrawLight(eye, true);
        _eyeLerping.AddEye(eye);

        var size = new Vector2i(resolution, resolution);
        var viewport = _clyde.CreateViewport(size);
        viewport.AutomaticRender = true;
        viewport.Eye = eyeComp.Eye;

        _pendingCapture = (viewport, eye);
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

        var (viewport, eye) = pending;

        viewport.RenderTarget.CopyPixelsToMemory<Rgba32>(image =>
        {
            using var output = new MemoryStream();
            image.SaveAsPng(output);

            RaiseNetworkEvent(new PhotoCaptureEvent(output.ToArray()));

            QueueDel(eye);
        });
    }

    public OwnedTexture GetPhoto(byte[] imageData)
    {
        using var ms = new MemoryStream(imageData);
        return _clyde.LoadTextureFromPNGStream(ms);
    }
}
