using System.IO;
using System.Numerics;
using Content.Client._RMC14.NewPlayer;
using Content.Shared._RMC14.Camera.PhotoCamera;
using Content.Shared._RMC14.Weapons.Common;
using Content.Shared.Coordinates.Helpers;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Shared.Player;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Content.Client._RMC14.Camera;

public sealed class RMCPhotoCameraSystem : SharedRmcPhotoCameraSystem
{
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly NewPlayerVisualizerSystem _newPlayerVisualizer = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;

    private (IClydeViewport viewport, EntityUid eye, NetEntity cameraUser)? _pendingCapture;
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
        if (!_map.MapExists(world.MapId))
            return;

        var center = TransformSystem.ToCoordinates(world).SnapToGrid();
        if (!center.IsValid(EntityManager))
            return;

        RaiseNetworkEvent(new RequestPhotoCaptureEvent(GetNetCoordinates(center)));
    }

    private void OnTakePhoto(TakePhotoEvent ev, EntitySessionEventArgs args)
    {
        if (!TryComp(GetEntity(ev.Camera), out RMCPhotoCameraComponent? cameraComp))
            return;

        if (_player.LocalEntity is not { } localEntity)
            return;

        var takingPhoto = EnsureComp<RMCTakingPhotoComponent>(localEntity);
        takingPhoto.PhotoCoordinates = TransformSystem.GetMoverCoordinates(GetEntity(ev.Eye));
        takingPhoto.ZoomMode = ev.ZoomMode;

        _newPlayerVisualizer.UpdateAllAppearance();

        TakePhoto(cameraComp.Resolution, ev.Eye, ev.CameraUser, ev.Zoom);
    }

    private void TakePhoto(int resolution, NetEntity netEye, NetEntity cameraUser, Vector2 zoom)
    {
        var eye = GetEntity(netEye);
        if (!TryComp(eye, out EyeComponent? eyeComp))
            return;

        EyeSystem.SetZoom(eye, zoom);

        var size = new Vector2i(resolution, resolution);
        var viewport = _clyde.CreateViewport(size);
        viewport.AutomaticRender = true;
        viewport.Eye = eyeComp.Eye;

        _pendingCapture = (viewport, eye, cameraUser);
    }

    public OwnedTexture GetPhoto(byte[] imageData)
    {
        using var ms = new MemoryStream(imageData);
        return _clyde.LoadTextureFromPNGStream(ms);
    }

    public bool InPhotoRange(EntityUid uid)
    {
        if (!TryComp(_player.LocalEntity, out RMCTakingPhotoComponent? takingPhoto))
            return false;

        if (takingPhoto.PhotoCoordinates is  not { } photoCoordinates)
            return false;

        var photoCaptureDistance = ((float)takingPhoto.ZoomMode * 2 + 1) / 2 * Math.Sqrt(2) + 1.5f;
        if (!photoCoordinates.TryDistance(EntityManager, TransformSystem.GetMoverCoordinates(uid), out var distance) || distance > photoCaptureDistance)
            return false;

        return true;
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

        var (viewport, eye, cameraUser) = pending;

        viewport.RenderTarget.CopyPixelsToMemory<Rgba32>(image =>
        {
            using var output = new MemoryStream();
            image.SaveAsPng(output);

            RaiseNetworkEvent(new PhotoCaptureEvent(output.ToArray(), GetNetEntity(eye), cameraUser));

            if (_player.LocalEntity is not { } localEntity)
                return;

            RemComp<RMCTakingPhotoComponent>(localEntity);
            _newPlayerVisualizer.UpdateAllAppearance();
        });
    }
}
