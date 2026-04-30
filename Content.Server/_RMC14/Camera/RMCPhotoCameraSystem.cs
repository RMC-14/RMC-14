using System.IO;
using System.Linq;
using System.Numerics;
using Content.Server._RMC14.Mentor;
using Content.Server.Administration.Managers;
using Content.Server.Examine;
using Content.Server.Players.JobWhitelist;
using Content.Shared._RMC14.Camera;
using Content.Shared._RMC14.Camera.PhotoCamera;
using Content.Shared.Hands.Components;
using Content.Shared.Roles;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Prototypes;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Content.Server._RMC14.Camera;

public sealed class RMCPhotoCameraSystem : SharedRmcPhotoCameraSystem
{
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly ExamineSystem _examine = default!;
    [Dependency] private readonly JobWhitelistManager _jobWhitelist = default!;
    [Dependency] private readonly MentorManager _mentorManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly ViewSubscriberSystem _viewSubscriber = default!;

    private const int MaxSize = 256 * 1024; // 256 KB
    private static readonly ProtoId<JobPrototype> CommandingOfficerJob = "CMCommandingOfficer";
    private static readonly ProtoId<JobPrototype> ProvostInspectorJob = "CMProvostInspector";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<RequestPhotoCaptureEvent>(OnRequestPhotoCapture);
        SubscribeNetworkEvent<PhotoCaptureEvent>(OnPhotoCaptured);
    }

    private void OnRequestPhotoCapture(RequestPhotoCaptureEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } sessionEntity)
            return;

        if (!TryGetCamera(sessionEntity, out var camera))
            return;

        var coordinates = GetCoordinates(ev.Coordinates);

        if (!coordinates.IsValid(EntityManager))
            return;

        if (!_examine.InRangeUnOccluded(sessionEntity, coordinates, 11))
            return;

        var eye = Spawn(null, TransformSystem.GetMoverCoordinates(camera.Value));

        var eyeComp = EnsureComp<EyeComponent>(eye);
        EnsureComp<RMCStaticZoomLevelComponent>(eye);

        var zoom = new Vector2(camera.Value.Comp.ZoomLevel, camera.Value.Comp.ZoomLevel);
        EyeSystem.SetZoom(eye, zoom, eyeComp);
        EyeSystem.SetDrawFov(eye, true, eyeComp);
        EyeSystem.UpdateEye((eye, eyeComp));
        EyeSystem.SetDrawLight(eye, true);

        var whiteListedSession = _adminManager.ActiveAdmins.FirstOrDefault() ?? _mentorManager.GetActiveMentors().FirstOrDefault();

        if (whiteListedSession == null)
        {
            foreach (var session in _playerManager.Sessions)
            {
                if (!_mentorManager.IsMentor(session.UserId) &&
                    !_jobWhitelist.IsWhitelisted(session.UserId, CommandingOfficerJob) &&
                    !_jobWhitelist.IsWhitelisted(session.UserId, ProvostInspectorJob))
                    continue;

                whiteListedSession = session;
                break;
            }
        }

        if (whiteListedSession != null)
        {
            _viewSubscriber.AddViewSubscriber(eye, whiteListedSession);
            camera.Value.Comp.ImageRenderedBy = whiteListedSession.UserId;

            var photoEv = new TakePhotoEvent(GetNetEntity(eye), GetNetEntity(camera.Value), GetNetEntity(sessionEntity), zoom, camera.Value.Comp.ZoomMode);
            RaiseNetworkEvent(photoEv, whiteListedSession);
        }
    }

    private void OnPhotoCaptured(PhotoCaptureEvent ev, EntitySessionEventArgs args)
    {
        if (!_adminManager.IsAdmin(args.SenderSession, true) && !_mentorManager.IsMentor(args.SenderSession.UserId))
            return;

        if (ev.ImageData.Length > MaxSize)
            return;

        if (!TryGetCamera(GetEntity(ev.CameraUser), out var camera))
            return;

        if (camera.Value.Comp.PhotoPrintedAt != null || camera.Value.Comp.ImageData != null)
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

            image.Mutate(x => x.Resize(camera.Value.Comp.Resolution, camera.Value.Comp.Resolution));

            using var output = new MemoryStream();
            image.SaveAsPng(output);

            camera.Value.Comp.PhotoPrintedAt = Timing.CurTime + camera.Value.Comp.PrintDelay;
            camera.Value.Comp.ImageData = output.ToArray();
            Dirty(camera.Value);

            Audio.PlayPvs(camera.Value.Comp.ShutterSound, camera.Value);
            _viewSubscriber.RemoveViewSubscriber(GetEntity(ev.Eye), args.SenderSession);
            QueueDel(GetEntity(ev.Eye));
        }
        catch
        {
            // Failed to load the image
        }
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
            Dirty(photo, photoComp);

            camera.PhotoPrintedAt = null;
            camera.ImageData = null;
            Dirty(uid, camera);

            if (_container.TryGetContainingContainer(uid, out var container))
            {
                if (TryComp(container.Owner, out HandsComponent? hands))
                    Hands.TryPickupAnyHand(container.Owner, photo, handsComp: hands);
            }
        }
    }
}

