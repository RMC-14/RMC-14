using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Shared.Actions;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Camera.PhotoCamera;

public abstract class SharedRmcPhotoCameraSystem : EntitySystem
{
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] protected readonly SharedEyeSystem EyeSystem = default!;
    [Dependency] protected readonly SharedHandsSystem Hands = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly SharedTransformSystem TransformSystem = default!;

    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCPhotoCameraComponent, GetItemActionsEvent>(OnGetItemActions);
        SubscribeLocalEvent<RMCPhotoCameraComponent, CycleCameraZoomActionEvent>(OnCycleZoom);
        SubscribeLocalEvent<RMCPhotoCameraComponent, ExaminedEvent>(OnCameraExamined);
        SubscribeLocalEvent<RMCPhotoCameraComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnGetItemActions(Entity<RMCPhotoCameraComponent> ent, ref GetItemActionsEvent args)
    {
        args.AddAction(ref ent.Comp.Action, ent.Comp.ActionId);
        Dirty(ent.Owner, ent.Comp);
    }

    private void OnCycleZoom(Entity<RMCPhotoCameraComponent> ent, ref CycleCameraZoomActionEvent args)
    {
        ent.Comp.ZoomMode++;
        if ((int)ent.Comp.ZoomMode > (int)PhotoZoomMode.Wide)
            ent.Comp.ZoomMode = PhotoZoomMode.Focused;

        ent.Comp.ZoomLevel = ent.Comp.BaseZoomLevel + (int)ent.Comp.ZoomMode * ent.Comp.ZoomStep;
        Dirty(ent);

        var captureSize = (int)ent.Comp.ZoomMode * 2 + 1;
        Popup.PopupClient(Loc.GetString("rmc-photo-camera-cycle-zoom", ("camera", ent.Owner), ("captureSize", captureSize)), args.Performer, args.Performer);
        Audio.PlayPredicted(ent.Comp.CycleZoomSound, args.Performer, args.Performer);

        args.Handled = true;
    }

    private void OnCameraExamined(Entity<RMCPhotoCameraComponent> ent, ref ExaminedEvent args)
    {
        var captureSize = (int)ent.Comp.ZoomMode * 2 + 1;
        args.PushMarkup(Loc.GetString(Loc.GetString("rmc-photo-camera-examine-text", ("captureSize", captureSize), ("charges", ent.Comp.RemainingCharges))), 1);
    }

    private void OnInteractUsing(Entity<RMCPhotoCameraComponent> ent, ref InteractUsingEvent args)
    {
        if (!TryComp(args.Used, out RMCPhotoCameraFilmComponent? cameraFilm))
            return;

        if (ent.Comp.RemainingCharges > 0)
        {
            Popup.PopupClient(Loc.GetString("rmc-photo-camera-film-insert-failed-full", ("camera", ent)), args.User, args.User, PopupType.SmallCaution);
            return;
        }

        args.Handled = true;

        ent.Comp.RemainingCharges += cameraFilm.PhotoCharges;
        Dirty(ent);

        Audio.PlayPredicted(ent.Comp.FilmInsertSound, ent, args.User);

        if (_net.IsServer)
            QueueDel(args.Used);
    }

    protected bool TryGetCamera(EntityUid uid, [NotNullWhen(true)] out Entity<RMCPhotoCameraComponent>? camera)
    {
        camera = null;

        var activeItem = Hands.GetActiveItem(uid);
        if (activeItem == null || !TryComp(activeItem, out RMCPhotoCameraComponent? cameraComponent))
            return false;

        camera = (activeItem.Value, cameraComponent);
        return true;
    }
}

[Serializable, NetSerializable]
public sealed class RequestPhotoCaptureEvent(NetCoordinates coordinates) : EntityEventArgs
{
    public NetCoordinates Coordinates = coordinates;
}

[Serializable, NetSerializable]
public sealed class TakePhotoEvent(NetEntity eye, NetEntity camera, NetEntity cameraUser, Vector2 zoom, PhotoZoomMode zoomMode) : EntityEventArgs
{
    public NetEntity Eye = eye;
    public NetEntity Camera = camera;
    public NetEntity CameraUser = cameraUser;
    public Vector2 Zoom = zoom;
    public PhotoZoomMode ZoomMode = zoomMode;
}

[Serializable, NetSerializable]
public sealed class PhotoCaptureEvent(byte[] imageData, NetEntity eye, NetEntity cameraUser) : EntityEventArgs
{
    public byte[] ImageData = imageData;
    public NetEntity Eye = eye;
    public NetEntity CameraUser = cameraUser;
}

[Serializable, NetSerializable]
public sealed class RequestStoredPhotoEvent(NetEntity photo) : EntityEventArgs
{
    public NetEntity Photo = photo;
}

[Serializable, NetSerializable]
public sealed class ReceivedStoredPhotoEvent(byte[] imageData, NetEntity photo) : EntityEventArgs
{
    public byte[] ImageData = imageData;
    public NetEntity Photo = photo;
}
