using System.Drawing;
using Content.Shared._RMC14.Dropship.AttachmentPoint;
using Content.Shared._RMC14.Dropship.ElectronicSystem;
using Content.Shared._RMC14.PowerLoader;
using Robust.Server.GameObjects;

namespace Content.Server._RMC14.Dropship.ElectronicSystem;

public sealed class DropshipElectronicSystemSystem : SharedDropshipElectronicSystemSystem
{
    [Dependency] private readonly PointLightSystem _pointLight = default!;

    protected override void OnDropShipAttachmentInserted(Entity<DropshipElectronicSystemPointComponent> ent,
        ref DropShipAttachmentInsertedEvent args)
    {
        base.OnDropShipAttachmentInserted(ent, ref args);
        if (TryComp(args.Inserted, out DropshipSpotlightComponent? spotlight))
        {
            var light = EnsureComp<PointLightComponent>(ent);
            _pointLight.SetEnabled(ent, spotlight.Enabled);
            _pointLight.SetRadius(ent, spotlight.Radius);
            _pointLight.SetEnergy(ent, spotlight.Energy);
            _pointLight.SetSoftness(ent, spotlight.Softness);
            Dirty(ent, light);
        }
    }

    protected override void OnDropShipAttachmentDetached(Entity<DropshipElectronicSystemPointComponent> ent,
        ref DropShipAttachmentDetachedEvent args)
    {
        base.OnDropShipAttachmentDetached(ent, ref args);
        RemComp<PointLightComponent>(ent);
    }
}
