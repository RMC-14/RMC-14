using Content.Server.GameTicking;
using Content.Server.Ghost.Roles;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Server.Station.Systems;
using Content.Shared._RMC14.Admin;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared.GameTicking;
using Robust.Shared.Player;

namespace Content.Server._RMC14.Ghost.Roles;

public sealed class RMCGhostRoleSystem : EntitySystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly GhostRoleSystem _ghostRole = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly RoleSystem _role = default!;
    [Dependency] private readonly StationSpawningSystem _spawning = default!;
    [Dependency] private readonly SquadSystem _squad = default!;
    [Dependency] private readonly StationSystem _stations = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GhostRoleRaffleComponent, GhostRoleRaffleEvent>(OnGhostRoleRaffle);

        SubscribeLocalEvent<RMCGhostRoleComponent, TakeGhostRoleEvent>(OnTakeover, before: [typeof(GhostRoleSystem)]);
    }

    /// <summary>
    ///     Subtract the current round duration from the time requirement and set that as the raffle countdown.
    /// </summary>
    private void OnGhostRoleRaffle(Entity<GhostRoleRaffleComponent> ent, ref GhostRoleRaffleEvent args)
    {
        var timeUntilRequirement = Math.Max(0, args.RoundTimeRequirement - _gameTicker.RoundDuration().TotalSeconds);
        args.CountDown = args.CountDown += TimeSpan.FromSeconds(timeUntilRequirement);
        args.Handled = true;
    }

    private void OnTakeover(Entity<RMCGhostRoleComponent> ent, ref TakeGhostRoleEvent args)
    {
        if (args.TookRole ||
            Paused(ent) ||
            !TryComp<GhostRoleComponent>(ent, out var ghost) ||
            !_transform.TryGetMapOrGridCoordinates(ent, out var coords))
        {
            return;
        }

        var stationUid = _stations.GetOwningStation(ent);
        var profile = _gameTicker.GetPlayerProfile(args.Player);
        var mobUid = _spawning.SpawnPlayerMob(coords.Value, ghost.JobProto, profile, stationUid);

        EntityManager.AddComponents(mobUid, ent.Comp.AddComponents);

        _ghostRole.GhostRoleInternalCreateMindAndTransfer(args.Player, ent, mobUid, ghost);
        if (_mind.TryGetMind(args.Player, out var mindId, out _))
            _role.MindAddJobRole(mindId, jobPrototype: ghost.JobProto);

        EnsureComp<RMCAdminSpawnedComponent>(mobUid);
        RemoveDuplicateSquadLeader(mobUid);

        if (ghost.JobProto is { } jobId)
        {
            var spawnEv = new PlayerSpawnCompleteEvent(mobUid, args.Player, jobId, true, true, 0, default, profile);
            RaiseLocalEvent(mobUid, spawnEv, true);
        }

        args.TookRole = true;

        if (ent.Comp.Remaining is not { } remaining)
            return;

        if (remaining > 1)
        {
            ent.Comp.Remaining = remaining - 1;
            args.MoreAvailable = true;
            return;
        }

        _ghostRole.UnregisterGhostRole((ent, ghost));
        QueueDel(ent);
    }

    private void RemoveDuplicateSquadLeader(EntityUid mob)
    {
        if (!HasComp<SquadLeaderComponent>(mob) ||
            !TryComp<AssignSquadComponent>(mob, out var assign) ||
            assign.Squad is not { } squadId ||
            !_squad.TryEnsureSquad(squadId, out var squad) ||
            !_squad.TryGetSquadLeader(squad, out _))
        {
            return;
        }

        RemComp<SquadLeaderComponent>(mob);
    }
}

/// <summary>
///     Raised when a raffle is being initiated
/// </summary>
[ByRefEvent]
public record struct GhostRoleRaffleEvent(TimeSpan CountDown, float RoundTimeRequirement, bool Handled = false);

/// <summary>
/// raised on a ghost role before a player takes it or joins its raffle, cancel it to stop them
/// </summary>
[ByRefEvent]
public record struct RMCTakeGhostRoleAttemptEvent(ICommonSession Player, bool Cancelled = false);
