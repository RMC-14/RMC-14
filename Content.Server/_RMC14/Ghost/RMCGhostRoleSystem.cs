using Content.Server.GameTicking;
using Content.Server.Ghost.Roles;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Server.Roles.Jobs;
using Content.Server.Station.Systems;
using Content.Shared._RMC14.Admin;
using Content.Shared.GameTicking;
using Robust.Server.Player;

namespace Content.Server._RMC14.Ghost;

public sealed partial class RMCGhostRoleSystem : EntitySystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly JobSystem _job = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly RoleSystem _role = default!;
    [Dependency] private readonly StationSpawningSystem _spawning = default!;
    [Dependency] private readonly StationSystem _stations = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCGhostRoleComponent, TakeGhostRoleEvent>(OnTakeover, before: [typeof(GhostRoleSystem)]);
        SubscribeLocalEvent<RMCGhostRoleCompleteComponent, ComponentStartup>(OnComplete);
    }

    private void OnTakeover(Entity<RMCGhostRoleComponent> ent, ref TakeGhostRoleEvent args)
    {
        if (args.TookRole)
            return;

        if (!_transform.TryGetMapOrGridCoordinates(ent, out var coords) ||
            !TryComp<GhostRoleComponent>(ent, out var ghost) ||
            !_mind.TryGetMind(args.Player, out var mindId, out var mindComp))
            return;

        var stationUid = _stations.GetOwningStation(ent);
        var profile = _gameTicker.GetPlayerProfile(args.Player);
        var mobUid = _spawning.SpawnPlayerMob(coords.Value, ghost.JobProto, profile, stationUid);

        _mind.TransferTo(mindId, mobUid, true, mind: mindComp);
        _role.MindAddJobRole(mindId, jobPrototype: ghost.JobProto);

        EntityManager.AddComponents(mobUid, ent.Comp.AddComponents);
        EnsureComp<RMCAdminSpawnedComponent>(mobUid);
        EnsureComp<RMCGhostRoleCompleteComponent>(mobUid);

        args.TookRole = true;

        if (ent.Comp.Remaining is { } remaining)
        {
            if (remaining <= 1)
            {
                Del(ent);
            }
            else
            {
                args.MoreAvailable = true;
                ent.Comp.Remaining = remaining - 1;
            }
        }
    }

    /// <summary>
    /// We have to give the system some time to process the added components before we can finalize it.
    /// </summary>
    private void OnComplete(Entity<RMCGhostRoleCompleteComponent> ent, ref ComponentStartup args)
    {
        if (_players.TryGetSessionByEntity(ent, out var player) &&
            _mind.TryGetMind(player, out var mind, out var _) &&
            _job.MindTryGetJobId(mind, out var job))
        {
            var profile = _gameTicker.GetPlayerProfile(player);

            var spawnEv = new PlayerSpawnCompleteEvent(
                ent,
                player,
                job,
                true,
                true,
                0,
                default,
                profile
            );
            RaiseLocalEvent(ent, spawnEv, true);
        }

        RemComp<RMCGhostRoleCompleteComponent>(ent);
    }
}
