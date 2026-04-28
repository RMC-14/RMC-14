using System.IO;
using Content.Shared._RMC14.Camera.PhotoCamera;
using Content.Shared.Hands.Components;
using Robust.Server.Audio;
using Robust.Server.Containers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Content.Server._RMC14.Camera;

public sealed class RMCPhotoCameraSystem : SharedRmcPhotoCameraSystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly ContainerSystem _container = default!;

    private const int MaxSize = 256 * 1024; // 256 KB

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<PhotoCaptureEvent>(OnPhotoCaptured);
    }

    private void OnPhotoCaptured(PhotoCaptureEvent ev, EntitySessionEventArgs args)
    {
        if (ev.ImageData.Length > MaxSize)
            return;

        if (args.SenderSession.AttachedEntity is not { } sessionEntity)
            return;

        if (!TryGetCamera(sessionEntity, out var camera))
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

            _audio.PlayPvs(camera.Value.Comp.ShutterSound, camera.Value);
        }
        catch
        {
            return;
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

