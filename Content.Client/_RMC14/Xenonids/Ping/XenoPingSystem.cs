using Content.Client._RMC14.Ping;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Evolution;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.HiveLeader;
using Content.Shared._RMC14.Xenonids.Ping;
using Robust.Shared.Configuration;

namespace Content.Client._RMC14.Xenonids.Ping;

public sealed class XenoPingSystem : RMCPingSystem<XenoPingEntityComponent, XenoPingDataComponent>
{
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;

    protected override bool ShouldShowPing(EntityUid pingUid, XenoPingEntityComponent pingComp)
    {
        if (LocalPlayer is not { } player || !CanViewXenoPings(player))
            return false;

        return pingComp.Hive != EntityUid.Invalid && _hive.IsMember(player, pingComp.Hive);
    }

    protected override bool ShouldShowPing(EntityUid pingCreator)
    {
        if (LocalPlayer is not { } player)
            return false;

        return CanSeePingCreator(player, pingCreator);
    }

    protected override bool ShouldCreateWaypoint(EntityUid pingUid, XenoPingEntityComponent pingComp)
    {
        if (!pingComp.ShowWaypoint)
            return false;

        return HasComp<XenoEvolutionGranterComponent>(pingComp.Creator) ||
               HasComp<HiveLeaderComponent>(pingComp.Creator);
    }

    public bool CanViewXenoPings(EntityUid player)
    {
        if (!_config.GetCVar(RMCCVars.RMCShowPings))
            return false;

        return HasComp<XenoComponent>(player) && HasComp<HiveMemberComponent>(player);
    }

    public bool CanSeePingCreator(EntityUid player, EntityUid pingCreator)
    {
        if (!CanViewXenoPings(player) || !HasComp<HiveMemberComponent>(pingCreator))
            return false;

        var playerHive = _hive.GetHive(player);
        var creatorHive = _hive.GetHive(pingCreator);
        return playerHive != null &&
               creatorHive != null &&
               playerHive.Value.Owner == creatorHive.Value.Owner;
    }

    public bool CanSeePing(EntityUid player, PingWaypointData waypoint)
    {
        if (!CanViewXenoPings(player))
            return false;

        if (TryComp<XenoPingEntityComponent>(waypoint.EntityUid, out var pingComp) &&
            pingComp.Hive != EntityUid.Invalid)
        {
            return _hive.IsMember(player, pingComp.Hive);
        }

        return CanSeePingCreator(player, waypoint.Creator);
    }
}
