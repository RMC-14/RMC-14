using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using Content.Client._RMC14.NewPlayer;
using Content.Client._RMC14.Photo;
using Content.Shared._RMC14.Camera.PhotoCamera;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Shared.Map;
using Robust.Shared.Player;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Content.Client._RMC14.Camera;

public sealed class RMCPhotoCameraSystem : SharedRmcPhotoCameraSystem
{
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly NewPlayerVisualizerSystem _newPlayerVisualizer = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;

    private (IClydeViewport viewport, NetEntity camera, List<EntityInPhoto> entities)? _pendingCapture;
    private bool _skipFrame;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<TakePhotoEvent>(OnTakePhoto);
        SubscribeNetworkEvent<ReceiveStoredPhotoEvent>(OnReceivedStoredPhoto);

        SubscribeLocalEvent<RMCPhotoCameraComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnAfterInteract(Entity<RMCPhotoCameraComponent> ent, ref AfterInteractEvent args)
    {
        if (!Timing.IsFirstTimePredicted || args.Handled)
            return;

        if (ent.Comp.PhotoPrintedAt != null)
            return;

        var world = _eye.PixelToMap(_inputManager.MouseScreenPosition);
        if (!_map.MapExists(world.MapId))
            return;

        var coordinates = TransformSystem.ToCoordinates(world);
        if (ent.Comp.AutoCenter)
            coordinates = coordinates.SnapToGrid();

        if (!coordinates.IsValid(EntityManager))
            return;

        if (ent.Comp.RemainingCharges <= 0)
        {
            if (_player.LocalEntity is { } localEntity)
                Popup.PopupClient(Loc.GetString("rmc-photo-camera-make-photo-failed-empty", ("camera", ent)), localEntity, localEntity, PopupType.SmallCaution);

            return;
        }

        RaiseNetworkEvent(new RequestPhotoCaptureEvent(GetNetCoordinates(coordinates)));
    }

    private void OnTakePhoto(TakePhotoEvent ev, EntitySessionEventArgs args)
    {
        if (!TryComp(GetEntity(ev.Camera), out RMCPhotoCameraComponent? cameraComp))
            return;

        var eye = GetEntity(cameraComp.Eye);
        if (!TryComp(eye, out EyeComponent? eyeComp))
            return;

        var photoCoordinates = TransformSystem.GetMoverCoordinates(eye.Value).Offset(eyeComp.Offset);

        if (_player.LocalEntity is { } localEntity)
        {
            var takingPhoto = EnsureComp<RMCTakingPhotoComponent>(localEntity);
            takingPhoto.PhotoCoordinates = photoCoordinates;
            takingPhoto.ZoomMode = ev.ZoomMode;
            takingPhoto.Offset = eyeComp.Offset;

            _newPlayerVisualizer.UpdateAllAppearance();
        }

        TakePhoto((eye.Value, eyeComp), cameraComp.Resolution, ev.Camera, ev.Zoom, ev.ZoomMode, photoCoordinates);
    }

    private void OnReceivedStoredPhoto(ReceiveStoredPhotoEvent ev, EntitySessionEventArgs args)
    {
        var photo = GetEntity(ev.Photo);
        if (!TryComp(photo, out RMCPhotoComponent? photoComp))
            return;

        if (!Timing.IsFirstTimePredicted)
            return;

        photoComp.ImageData = ev.ImageData;

        if (_uiSystem.TryGetOpenUi(photo, RMCPhotoUi.Key, out PhotoBui? bui))
        {
            bui.Refresh();
        }
    }

    private void TakePhoto(Entity<EyeComponent> eye, int resolution, NetEntity camera, Vector2 zoom, PhotoZoomMode zoomMode, EntityCoordinates photoCoordinates)
    {
        EyeSystem.SetZoom(eye, zoom);

        var size = new Vector2i(resolution, resolution);
        var viewport = _clyde.CreateViewport(size);
        viewport.AutomaticRender = true;
        viewport.Eye = eye.Comp.Eye;

        var photoArea = GetPhotoAreaRange(zoomMode);
        var visibleEntities = GetVisibleEntities(eye, photoCoordinates, photoArea); // Doing this here instead of on the server so it's more accurate.

        List<EntityInPhoto> entitiesInPhoto = new();
        foreach (var entity in visibleEntities)
        {
            if (!TryComp(entity, out HandsComponent? hands))
                continue;

            var heldItems = new List<NetEntity>();
            foreach (var hand in hands.Hands)
            {
                var heldItem = Hands.GetHeldItem((entity, hands), hand.Key);
                if (heldItem == null)
                    continue;

                heldItems.Add(GetNetEntity(heldItem.Value));
            }

            entitiesInPhoto.Add(new EntityInPhoto(GetNetEntity(entity), heldItems));
        }

        _pendingCapture = (viewport, camera, entitiesInPhoto);
    }

    private HashSet<EntityUid> GetVisibleEntities(Entity<EyeComponent> eye, EntityCoordinates photoCoords, float range)
    {
        var visibleEntities = new HashSet<EntityUid>();
        photoCoords.TryDistance(EntityManager, TransformSystem.GetMoverCoordinates(eye), out var cameraDistance);
        var occludeRange = range + cameraDistance;

        foreach (var marine in _entityLookup.GetEntitiesInRange<MarineComponent>(photoCoords, range, LookupFlags.Dynamic | LookupFlags.Uncontained))
        {
            if (!photoCoords.TryDistance(EntityManager, TransformSystem.GetMoverCoordinates(marine), out var distance) || distance > range)
                continue;

            if (!Examine.InRangeUnOccluded(eye, marine, occludeRange))
                continue;

            visibleEntities.Add(marine);
        }

        foreach (var xeno in _entityLookup.GetEntitiesInRange<XenoComponent>(photoCoords, range, LookupFlags.Dynamic | LookupFlags.Uncontained))
        {
            if (!photoCoords.TryDistance(EntityManager, TransformSystem.GetMoverCoordinates(xeno), out var distance) || distance > range)
                continue;

            if (!Examine.InRangeUnOccluded(eye, xeno, occludeRange))
                continue;

            visibleEntities.Add(xeno);
        }

        return visibleEntities;
    }

    public bool TryGetPhoto(EntityUid photo, [NotNullWhen(true)] out OwnedTexture? photoTexture, out string photoName)
    {
        photoName = "";
        photoTexture = null;

        if (!TryComp(photo, out RMCPhotoComponent? photoComp))
            return false;

        photoName = photoComp.PhotoName;

        if (TryComp(photo, out RMCCachedPhotoComponent? cachedPhoto))
        {
            photoTexture = cachedPhoto.CachedPhoto;
            if (photoTexture != null)
                return true;
        }

        if (photoComp.ImageData == null)
            return false;

        using var ms = new MemoryStream(photoComp.ImageData);
        photoTexture = _clyde.LoadTextureFromPNGStream(ms);

        var cachedPhotoComp = EnsureComp<RMCCachedPhotoComponent>(photo);
        cachedPhotoComp.CachedPhoto = photoTexture;

        return true;
    }

    public void RequestPhoto(EntityUid photo)
    {
        if (!TryComp(photo, out RMCPhotoComponent? comp))
            return;

        if (comp.ImageData != null)
            return;

        RaiseNetworkEvent(new RequestStoredPhotoEvent(GetNetEntity(photo)));
    }

    public bool InPhotoRange(EntityUid uid)
    {
        if (!TryComp(_player.LocalEntity, out RMCTakingPhotoComponent? takingPhoto))
            return false;

        if (takingPhoto.PhotoCoordinates is  not { } photoCoordinates)
            return false;

        var photoCaptureDistance = GetPhotoAreaRange(takingPhoto.ZoomMode) + 1.5f;
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

        var (viewport, camera, entitiesInPhoto) = pending;

        viewport.RenderTarget.CopyPixelsToMemory<Rgba32>(image =>
        {
            var img = image.Clone();

            Task.Run(() =>
            {
                using (img)
                {
                    using var output = new MemoryStream();
                    img.SaveAsPng(output);

                    RaiseNetworkEvent(new PhotoCaptureEvent(output.ToArray(), camera, entitiesInPhoto));
                }
            });

            viewport.Dispose();

            if (_player.LocalEntity is not { } localEntity)
                return;

            RemComp<RMCTakingPhotoComponent>(localEntity);
            _newPlayerVisualizer.UpdateAllAppearance();
        });
    }
}
