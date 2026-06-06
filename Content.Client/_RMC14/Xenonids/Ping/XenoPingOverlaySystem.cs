using Content.Client._RMC14.Ping;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.Inventory.Events;

namespace Content.Client._RMC14.Xenonids.Ping;

public sealed class XenoPingOverlaySystem : RMCPingOverlaySystem<XenoComponent, XenoPingWaypointOverlay>
{
    protected override XenoPingWaypointOverlay CreateOverlay()
    {
        return new XenoPingWaypointOverlay();
    }

    protected override void OnRefreshComponentHud(Entity<XenoComponent> ent, ref RefreshEquipmentHudEvent<XenoComponent> args)
    {
        if (HasComp<HiveMemberComponent>(ent.Owner))
        {
            args.Active = true;
            args.Components.Add(ent.Comp);
        }
    }
}
