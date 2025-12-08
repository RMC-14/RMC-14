using Content.Client.Overlays;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Ping;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Client.Graphics;

namespace Content.Client._RMC14.Xenonids.Ping;

public sealed class XenoPingOverlaySystem : EquipmentHudSystem<XenoComponent>
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;

    private PingWaypointOverlay _overlay = default!;

    protected override SlotFlags TargetSlots => SlotFlags.NONE;

    public override void Initialize()
    {
        base.Initialize();
        _overlay = new PingWaypointOverlay();
    }

    protected override void OnRefreshComponentHud(Entity<XenoComponent> ent, ref RefreshEquipmentHudEvent<XenoComponent> args)
    {
        if (HasComp<HiveMemberComponent>(ent.Owner))
        {
            args.Active = true;
            args.Components.Add(ent.Comp);
        }
    }

    protected override void UpdateInternal(RefreshEquipmentHudEvent<XenoComponent> component)
    {
        base.UpdateInternal(component);

        if (!_overlayManager.HasOverlay<PingWaypointOverlay>())
        {
            _overlayManager.AddOverlay(_overlay);
        }
    }

    protected override void DeactivateInternal()
    {
        base.DeactivateInternal();
        _overlayManager.RemoveOverlay(_overlay);
    }
}
