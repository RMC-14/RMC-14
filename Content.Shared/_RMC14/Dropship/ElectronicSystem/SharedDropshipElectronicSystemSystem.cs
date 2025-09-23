using Content.Shared._RMC14.Camera;
using Content.Shared._RMC14.Dropship.AttachmentPoint;
using Content.Shared._RMC14.Dropship.Weapon;
using Content.Shared._RMC14.PowerLoader;
using Robust.Shared.Containers;

namespace Content.Shared._RMC14.Dropship.ElectronicSystem;

public abstract class SharedDropshipElectronicSystemSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDropshipSystem _dropship = default!;
    [Dependency] private readonly SharedRMCCameraSystem _rmcCamera = default!;

    private const int MinSpread = 0;
    private const int MinBulletSpread = 1;
    private const float MinTravelTime = 1;

    public override void Initialize()
    {
        SubscribeLocalEvent<DropshipComponent, DropshipWeaponShotEvent>(OnDropshipWeaponShot);

        SubscribeLocalEvent<DropshipElectronicSystemPointComponent, DropShipAttachmentInsertedEvent>(OnDropShipAttachmentInserted);
        SubscribeLocalEvent<DropshipElectronicSystemPointComponent, DropShipAttachmentDetachedEvent>(OnDropShipAttachmentDetached);
    }

    private void OnDropshipWeaponShot(Entity<DropshipComponent> ent, ref DropshipWeaponShotEvent args)
    {
        foreach (var point in ent.Comp.AttachmentPoints)
        {
            if (!TryComp(point, out DropshipElectronicSystemPointComponent? electronic) ||
                !_container.TryGetContainer(point, electronic.ContainerId, out var container))
                continue;

            foreach (var contained in container.ContainedEntities)
            {
                if (!TryComp(contained, out DropshipTargetingSystemComponent? targeting))
                    continue;

                args.Spread = Math.Max(MinSpread, args.Spread + targeting.SpreadModifier);
                args.BulletSpread = Math.Max(MinBulletSpread, args.BulletSpread + targeting.BulletSpreadModifier);
                args.TravelTime = TimeSpan.FromSeconds(Math.Max(MinTravelTime, args.TravelTime.TotalSeconds + targeting.TravelingTimeModifier.TotalSeconds));
            }
        }
    }

    protected virtual void OnDropShipAttachmentInserted(Entity<DropshipElectronicSystemPointComponent> ent, ref DropShipAttachmentInsertedEvent args)
    {
        if (!_dropship.TryGetGridDropship(ent, out var dropship))
            return;

        if (TryComp(args.Inserted, out CameraSignalGranterComponent? signalGranter))
            ModifyCameraSignals((args.Inserted, signalGranter), dropship);
    }

    protected virtual void OnDropShipAttachmentDetached(Entity<DropshipElectronicSystemPointComponent> ent, ref DropShipAttachmentDetachedEvent args)
    {
        if (!_dropship.TryGetGridDropship(ent, out var dropship))
            return;

        if (TryComp(args.Detached, out CameraSignalGranterComponent?  signalGranter))
            ModifyCameraSignals((args.Detached, signalGranter), dropship, true);

        if (TryComp(args.Detached, out DropshipSpotlightComponent? spotlight))
        {
            spotlight.Enabled = false;
            Dirty(args.Detached, spotlight);
        }
    }

    private void ModifyCameraSignals(Entity<CameraSignalGranterComponent> ent, Entity<DropshipComponent> dropship, bool remove = false)
    {
        var query = Transform(dropship).ChildEnumerator;
        while (query.MoveNext(out var uid))
        {
            if (!TryComp(uid, out RMCCameraComputerComponent? cameraComputer))
                continue;

            foreach (var protoId in ent.Comp.ProtoIds)
            {
                if (remove)
                    _rmcCamera.RemoveProtoId(cameraComputer, protoId);
                else
                {
                    _rmcCamera.AddProtoId(cameraComputer, protoId);
                }
                _rmcCamera.RefreshCameras(protoId);
            }
            Dirty(uid, cameraComputer);
        }
    }
}
