using Content.Client._RMC14.Ping;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Evolution;
using Content.Shared._RMC14.Xenonids.HiveLeader;

namespace Content.Client._RMC14.Xenonids.Ping;

public sealed class XenoPingWaypointOverlay : RMCPingWaypointOverlay
{
    private readonly XenoPingSystem _ping;

    public XenoPingWaypointOverlay()
    {
        _ping = Entity.System<XenoPingSystem>();
    }

    protected override IReadOnlyDictionary<EntityUid, PingWaypointData> GetWaypoints()
    {
        return _ping.GetPingWaypoints();
    }

    protected override bool CanViewWaypoints(EntityUid player)
    {
        return _ping.CanViewXenoPings(player);
    }

    protected override bool ShouldIncludeWaypoint(PingWaypointData waypoint, EntityUid player)
    {
        return _ping.CanSeePing(player, waypoint);
    }

    protected override Color GetCreatorTextColor(EntityUid creator)
    {
        if (!Entity.EntityExists(creator))
            return Color.White;

        if (Entity.HasComponent<XenoEvolutionGranterComponent>(creator))
            return Color.FromHex("#D8B4FF");

        if (Entity.HasComponent<HiveLeaderComponent>(creator))
            return Color.Orange;

        return Color.LightGray;
    }

    protected override int GetWaypointPriority(PingWaypointData waypoint)
    {
        if (Entity.HasComponent<XenoEvolutionGranterComponent>(waypoint.Creator))
            return 0;
        if (Entity.HasComponent<HiveLeaderComponent>(waypoint.Creator))
            return 1;

        return 2;
    }

    protected override float GetWaypointRadius(PingWaypointData waypoint)
    {
        var radius = base.GetWaypointRadius(waypoint);
        if (Entity.HasComponent<XenoEvolutionGranterComponent>(waypoint.Creator))
            radius *= 1.1f;

        return radius;
    }
}
