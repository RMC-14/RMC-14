using Content.Server.Roles.Jobs;
using Content.Server.Warps;
using Content.Shared._RMC14.Ghost;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
using Robust.Server.Player;
using Robust.Shared.Map;
using System.Linq;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.Marines;

namespace Content.Server._RMC14.Ghost
{
    public sealed class RMCGhostSystemHelper : EntitySystem
    {
        [Dependency] private readonly IEntityManager _entManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly MobStateSystem _mobState = default!;
        [Dependency] private readonly JobSystem _jobs = default!;

        private readonly List<MapId> _planetMaps = new();
        private readonly List<MapId> _warshipMaps = new();

        private void GetMapAreas()
        {
            // TODO: implement logic for evacuated marines.

            var planetQuery = EntityQueryEnumerator<RMCPlanetComponent, TransformComponent>();
            while (planetQuery.MoveNext(out _, out var xform))
            {
                _planetMaps.Add(xform.MapID);
            }

            var warshipQuery = EntityQueryEnumerator<AlmayerComponent, TransformComponent>();
            while (warshipQuery.MoveNext(out _, out var xform))
            {
                _warshipMaps.Add(xform.MapID);
            }
        }

        public List<RMCGhostWarp> GetWarps(EntityUid entity)
        {
            GetMapAreas();
            return GetPlayerWarps(entity).Concat(GetLocationWarps()).ToList();
        }

        private string GetEntityAreaName(EntityUid uid)
        {
            var area = Loc.GetString("rmc-ghost-warp-unknown");
            if (_entManager.TryGetComponent<TransformComponent>(uid, out var xform))
            {
                var mapId = xform.MapID;
                if (_warshipMaps.Contains(mapId))
                {
                    area = Loc.GetString("rmc-ghost-location-warp-shipside");
                }
                else if (_planetMaps.Contains(mapId))
                {
                    area = Loc.GetString("rmc-ghost-location-warp-groundside");
                }
            }
            return area;
        }
        public IEnumerable<RMCGhostWarp> GetLocationWarps()
        {

            var allQuery = AllEntityQuery<WarpPointComponent>();

            while (allQuery.MoveNext(out var uid, out var warp))
            {
                var area = GetEntityAreaName(uid);
                yield return new RMCGhostWarp(GetNetEntity(uid), warp.Location ?? Name(uid), area, true, null, null); // locations will fall under the other cateogry
            }
        }

        public IEnumerable<RMCGhostWarp> GetPlayerWarps(EntityUid except)
        {
            foreach (var player in _playerManager.Sessions)
            {
                if (player.AttachedEntity is not { Valid: true } attached)
                    continue;

                if (attached == except) continue;

                TryComp<MindContainerComponent>(attached, out var mind);

                if (!_mobState.IsAlive(attached) && !_mobState.IsCritical(attached)) continue;

                var area = GetEntityAreaName(attached);
                if (_jobs.MindTryGetJob(mind?.Mind, out var jobProto))
                {

                    var entityName = Name(attached);
                    var jobName = _jobs.MindTryGetJobName(mind?.Mind);

                    var playerInfo = Loc.GetString("rmc-ghost-warp-player-info-unknown-squad",
                    ("entityName", entityName),
                    ("jobName", jobName));

                    if (_jobs.TryGetDepartment(jobProto.ID, out var departmentProto))
                    {
                        var warpColor = departmentProto.Color;

                        if (TryComp(attached, out SquadMemberComponent? member))
                        {
                            if (TryComp(member.Squad, out SquadTeamComponent? squadComp))
                            {
                                var squadName = member.Squad == null ? Loc.GetString("rmc-ghost-warp-unknown") : Name(member.Squad.Value);
                                warpColor = squadComp.Color;
                                playerInfo = Loc.GetString("rmc-ghost-warp-player-info-known-squad",
                                ("entityName", entityName),
                                ("jobName", jobName),
                                ("squadName", squadName));
                            }
                        }
                        yield return new RMCGhostWarp(GetNetEntity(attached), playerInfo, area, false, departmentProto.CustomName ?? Loc.GetString(departmentProto.Name), warpColor.ToHex());
                    }
                    else
                    {
                        yield return new RMCGhostWarp(GetNetEntity(attached), playerInfo, area, false, null, null); // no department was found, i'm probably going to need to create more departments, pmc and such.
                    }
                }
            }
        }
    }
}
