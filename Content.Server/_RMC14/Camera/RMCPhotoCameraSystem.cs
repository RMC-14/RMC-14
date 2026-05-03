using System.IO;
using System.Linq;
using System.Numerics;
using Content.Server._RMC14.Mentor;
using Content.Server.Administration.Managers;
using Content.Server.Players.JobWhitelist;
using Content.Shared._RMC14.Camera;
using Content.Shared._RMC14.Camera.PhotoCamera;
using Content.Shared._RMC14.Hands;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Damage;
using Content.Shared.Hands.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Spawners;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace Content.Server._RMC14.Camera;

public sealed class RMCPhotoCameraSystem : SharedRmcPhotoCameraSystem
{
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly JobWhitelistManager _jobWhitelist = default!;
    [Dependency] private readonly MentorManager _mentorManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly RMCHandsSystem _rmcHands = default!;
    [Dependency] private readonly ViewSubscriberSystem _viewSubscriber = default!;

    private const int MaxSize = 256 * 1024; // 256 KB
    private const float HurtThreshold = 0.25f;

    private static readonly ProtoId<JobPrototype> CommandingOfficerJob = "CMCommandingOfficer";
    private static readonly ProtoId<JobPrototype> ProvostInspectorJob = "CMProvostInspector";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<RequestPhotoCaptureEvent>(OnRequestPhotoCapture);
        SubscribeNetworkEvent<PhotoCaptureEvent>(OnPhotoCaptured);
        SubscribeNetworkEvent<RequestStoredPhotoEvent>(OnRequestStoredPhoto);
        SubscribeNetworkEvent<RequestStoredPhotoDescriptionEvent>(OnRequestStoredPhotoDescription);
    }

    private void OnRequestPhotoCapture(RequestPhotoCaptureEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } sessionEntity)
            return;

        if (!TryGetCamera(sessionEntity, out var camera))
            return;

        if (camera.Value.Comp.PhotoPrintedAt != null)
            return;

        if (camera.Value.Comp.RemainingCharges <= 0)
        {
            Popup.PopupClient(Loc.GetString("rmc-photo-camera-make-photo-failed-empty", ("camera", camera)), sessionEntity, sessionEntity);
            return;
        }

        var coordinates = GetCoordinates(ev.Coordinates);
        if (!coordinates.IsValid(EntityManager))
            return;

        if (!Examine.InRangeUnOccluded(sessionEntity, coordinates, camera.Value.Comp.Range))
            return;

        var cameraCoordinates = TransformSystem.GetMoverCoordinates(sessionEntity);
        if (camera.Value.Comp.AutoCenter)
            cameraCoordinates = cameraCoordinates.SnapToGrid();

        var eye = Spawn(null, cameraCoordinates);

        camera.Value.Comp.Eye = GetNetEntity(eye);
        Dirty(camera.Value);

        var eyeComp = EnsureComp<EyeComponent>(eye);
        EnsureComp<RMCStaticZoomLevelComponent>(eye);
        var timedDespawn = EnsureComp<TimedDespawnComponent>(eye);  // In case the client fails to send an image to the server.
        timedDespawn.Lifetime = 5;

        var zoom = new Vector2(camera.Value.Comp.ZoomLevel, camera.Value.Comp.ZoomLevel);
        EyeSystem.SetZoom(eye, zoom, eyeComp);
        EyeSystem.SetDrawFov(eye, true, eyeComp);
        EyeSystem.UpdateEye((eye, eyeComp));
        EyeSystem.SetDrawLight(eye, true);

        var offset = coordinates.Position - cameraCoordinates.Position;
        EyeSystem.SetOffset(eye, offset, eyeComp);

        var whiteListedSession = _adminManager.AllAdmins.FirstOrDefault();

        if (whiteListedSession == null)
        {
            foreach (var session in _playerManager.Sessions)
            {
                var isWhitelisted = _mentorManager.IsMentor(session.UserId) ||
                                    _jobWhitelist.IsWhitelisted(session.UserId, CommandingOfficerJob) ||
                                    _jobWhitelist.IsWhitelisted(session.UserId, ProvostInspectorJob);

                if (!isWhitelisted)
                    continue;

                if (session.AttachedEntity == null) // Prioritize players that are in the lobby.
                {
                    whiteListedSession = session;
                    break;
                }

                whiteListedSession ??= session;
            }
        }

        if (whiteListedSession != null)
        {
            _viewSubscriber.AddViewSubscriber(eye, whiteListedSession);
            camera.Value.Comp.ImageRenderedBy = whiteListedSession.UserId;

            var photoEv = new TakePhotoEvent(GetNetEntity(camera.Value), zoom, camera.Value.Comp.ZoomMode);
            RaiseNetworkEvent(photoEv, whiteListedSession);

            return;
        }

        Popup.PopupEntity(Loc.GetString("rmc-photo-camera-make-photo-failed"), sessionEntity, sessionEntity, PopupType.SmallCaution);
    }

    private void OnPhotoCaptured(PhotoCaptureEvent ev, EntitySessionEventArgs args)
    {
        if (!_adminManager.IsAdmin(args.SenderSession, true) &&
            !_mentorManager.IsMentor(args.SenderSession.UserId) &&
            !_jobWhitelist.IsWhitelisted(args.SenderSession.UserId, CommandingOfficerJob) &&
            !_jobWhitelist.IsWhitelisted(args.SenderSession.UserId, ProvostInspectorJob))
            return;

        var camera = GetEntity(ev.Camera);
        if (!TryComp<RMCPhotoCameraComponent>(camera, out var cameraComp))
            return;

        var eyeEntity = GetEntity(cameraComp.Eye);
        if (eyeEntity == null)
            return;

        _viewSubscriber.RemoveViewSubscriber(eyeEntity.Value, args.SenderSession);
        QueueDel(eyeEntity);

        cameraComp.Eye = null;
        Dirty(camera, cameraComp);

        if (ev.ImageData.Length > MaxSize)
            return;

        if (cameraComp.PhotoPrintedAt != null || cameraComp.ImageData != null)
            return;

        try
        {
            using var input = new MemoryStream(ev.ImageData);

            var format = Image.DetectFormat(input);
            if (!format.Equals(PngFormat.Instance))
                return;

            using var image = Image.Load<Rgba32>(input);
            image.Metadata.ExifProfile = null;
            image.Metadata.IccProfile = null;

            using var output = new MemoryStream();
            image.SaveAsPng(output);

            cameraComp.PhotoPrintedAt = Timing.CurTime + cameraComp.PrintDelay;
            cameraComp.ImageData = output.ToArray();
            cameraComp.EntitiesInPhoto = ev.EntitiesInPhoto.ToList();
            Dirty(camera, cameraComp);

            Audio.PlayPvs(cameraComp.ShutterSound, camera);
        }
        catch
        {
            // Failed to load the image
        }
    }

    private void OnRequestStoredPhoto(RequestStoredPhotoEvent ev, EntitySessionEventArgs args)
    {
        if (!TryComp(GetEntity(ev.Photo), out RMCPhotoComponent? photo) || photo.ImageData == null)
            return;

        RaiseNetworkEvent(new ReceiveStoredPhotoEvent(photo.ImageData, ev.Photo), args.SenderSession);
    }

    private void OnRequestStoredPhotoDescription(RequestStoredPhotoDescriptionEvent ev, EntitySessionEventArgs args)
    {
        if (!TryComp(GetEntity(ev.Photo), out RMCPhotoComponent? photo))
            return;

        if (args.SenderSession.AttachedEntity is not { } attachedEntity)
            return;

        var examineText = GetPhotoDescription(photo.EntitiesInPhoto, attachedEntity);

        RaiseNetworkEvent(new ReceiveStoredPhotoDescriptionEvent(ev.Photo, examineText), args.SenderSession);
    }

    private List<string> GetPhotoDescription(List<EntityInPhoto> entitiesInPhoto, EntityUid user)
    {
        List<string> photoText = new();
        foreach (var entity in entitiesInPhoto)
        {
            var uid = GetEntity(entity.Entity);
            var name = Identity.Name(uid, EntityManager, user);

            var description = GetVisibilityText(uid, name);

            var items = entity.HeldItems.Select(GetEntity).ToList();
            var holdingText = _rmcHands.GetExamineText(uid, user, items);

            photoText.Add($"{description} {holdingText}".Trim());
        }

        return photoText;
    }

    protected override List<string> GetExamineText(RMCPhotoComponent photo, EntityUid user)
    {
        return GetPhotoDescription(photo.EntitiesInPhoto, user);
    }

    private string GetVisibilityText(EntityUid uid, string name)
    {
        var text = Loc.GetString("rmc-photo-camera-entity-in-photo-entity-see", ("name", name));

        if (!TryComp(uid, out DamageableComponent? damageable) ||
            !TryComp(uid, out MobThresholdsComponent? thresholds))
            return text;

        foreach (var (threshold, state) in thresholds.Thresholds)
        {
            if (state != MobState.Dead)
                continue;

            if (damageable.TotalDamage < threshold * HurtThreshold)
                continue;

            text = Loc.GetString("rmc-photo-camera-entity-in-photo-entity-see-damaged", ("entity", uid), ("name", name));
            break;
        }

        return text;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<RMCPhotoCameraComponent>();

        while (query.MoveNext(out var uid, out var camera))
        {
            if (camera.PhotoPrintedAt == null || Timing.CurTime < camera.PhotoPrintedAt.Value)
                continue;

            var photo = SpawnAtPosition(camera.PhotoPrototype, TransformSystem.GetMoverCoordinates(uid));
            var photoComp = EnsureComp<RMCPhotoComponent>(photo);

            photoComp.ImageData = camera.ImageData;
            photoComp.RenderedBy = camera.ImageRenderedBy;
            photoComp.EntitiesInPhoto = camera.EntitiesInPhoto.ToList();
            Dirty(photo, photoComp);

            camera.PhotoPrintedAt = null;
            camera.ImageData = null;
            camera.RemainingCharges -= 1;
            camera.ImageRenderedBy = null;
            camera.EntitiesInPhoto.Clear();
            Dirty(uid, camera);

            if (_container.TryGetContainingContainer(uid, out var container))
            {
                if (TryComp(container.Owner, out HandsComponent? hands))
                    Hands.TryPickupAnyHand(container.Owner, photo, handsComp: hands);
            }
        }
    }
}

