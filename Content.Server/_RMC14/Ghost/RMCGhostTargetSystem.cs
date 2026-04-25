using System.Linq;
using System.Numerics;
using Content.Server.Administration.Logs;
using Content.Server.Body.Components;
using Content.Server.Roles.Jobs;
using Content.Server.Warps;
using Content.Shared._RMC14.Ghost;
using Content.Shared.Database;
using Content.Shared.Damage;
using Content.Shared.Follower;
using Content.Shared.Follower.Components;
using Content.Shared.Ghost;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Warps;
using Robust.Server.GameObjects;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;

namespace Content.Server._RMC14.Ghost;

public sealed class RMCGhostTargetSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly FollowerSystem _followerSystem = default!;
    [Dependency] private readonly JobSystem _jobs = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;

    private EntityQuery<GhostComponent> _ghostQuery;
    private EntityQuery<PhysicsComponent> _physicsQuery;

    public override void Initialize()
    {
        base.Initialize();

        _ghostQuery = GetEntityQuery<GhostComponent>();
        _physicsQuery = GetEntityQuery<PhysicsComponent>();

        SubscribeNetworkEvent<RMCGhostWarpsRequestEvent>(OnGhostWarpsRequest);
        SubscribeNetworkEvent<RMCGhostWarpToTargetRequestEvent>(OnGhostWarpToTargetRequest);
        SubscribeNetworkEvent<RMCGhostnadoRequestEvent>(OnGhostnadoRequest);
    }

    private void OnGhostWarpsRequest(RMCGhostWarpsRequestEvent msg, EntitySessionEventArgs args)
    {
        if (!TryGetSenderGhost(args, out var ghost))
        {
            Log.Warning($"User {args.SenderSession.Name} sent a {nameof(RMCGhostWarpsRequestEvent)} without being a ghost.");
            return;
        }

        var response = new RMCGhostWarpsResponseEvent(GetPlayerWarps(ghost).Concat(GetLocationWarps()).ToList());
        RaiseNetworkEvent(response, args.SenderSession.Channel);
    }

    private void OnGhostWarpToTargetRequest(RMCGhostWarpToTargetRequestEvent msg, EntitySessionEventArgs args)
    {
        if (!TryGetSenderGhost(args, out var ghost))
        {
            Log.Warning($"User {args.SenderSession.Name} tried to RMC ghost warp without being a ghost.");
            return;
        }

        var target = GetEntity(msg.Target);
        if (!Exists(target))
        {
            Log.Warning($"User {args.SenderSession.Name} tried to RMC ghost warp to an invalid entity id: {msg.Target}");
            return;
        }

        WarpTo(ghost, target);
    }

    private void OnGhostnadoRequest(RMCGhostnadoRequestEvent msg, EntitySessionEventArgs args)
    {
        if (!TryGetSenderGhost(args, out var ghost))
        {
            Log.Warning($"User {args.SenderSession.Name} tried to RMC ghostnado without being a ghost.");
            return;
        }

        if (_followerSystem.GetMostGhostFollowed() is not { } target)
            return;

        WarpTo(ghost, target);
    }

    private bool TryGetSenderGhost(EntitySessionEventArgs args, out EntityUid ghost)
    {
        ghost = default;

        if (args.SenderSession.AttachedEntity is not { Valid: true } attached ||
            !_ghostQuery.HasComp(attached))
        {
            return false;
        }

        ghost = attached;
        return true;
    }

    private IEnumerable<RMCGhostWarp> GetLocationWarps()
    {
        var allQuery = AllEntityQuery<WarpPointComponent>();

        while (allQuery.MoveNext(out var uid, out var warp))
        {
            yield return new RMCGhostWarp(
                GetNetEntity(uid),
                warp.Location ?? Name(uid),
                null,
                true,
                GetFollowerCount(uid),
                null,
                -1);
        }
    }

    private IEnumerable<RMCGhostWarp> GetPlayerWarps(EntityUid except)
    {
        var query = EntityQueryEnumerator<MetaDataComponent, MindContainerComponent>();
        while (query.MoveNext(out var uid, out var meta, out var mindContainer))
        {
            if (uid == except)
                continue;

            if (HasComp<BrainComponent>(uid) ||
                HasComp<BorgBrainComponent>(uid) ||
                HasComp<MMIComponent>(uid))
            {
                continue;
            }

            if (!mindContainer.EverHadMind)
                continue;

            var health = GetHealthStatus(uid);
            var jobName = _jobs.MindTryGetJobName(mindContainer.Mind);
            yield return new RMCGhostWarp(
                GetNetEntity(uid),
                meta.EntityName,
                jobName,
                false,
                GetFollowerCount(uid),
                health.State,
                health.Percent);
        }
    }

    private (string? State, int Percent) GetHealthStatus(EntityUid uid)
    {
        if (!_mobState.IsCritical(uid) && !_mobState.IsAlive(uid))
            return (null, -1);

        if (!TryComp<DamageableComponent>(uid, out var damageable) ||
            !TryComp<MobThresholdsComponent>(uid, out var thresholds) ||
            !_mobThreshold.TryGetThresholdForState(uid, MobState.Dead, out var deadThreshold, thresholds))
        {
            return (null, -1);
        }

        var maxHealth = deadThreshold.Value.Float();
        if (maxHealth <= 0)
            return (null, -1);

        var currentHealth = maxHealth - damageable.TotalDamage.Float();
        var percent = Math.Clamp((int) MathF.Round(currentHealth / maxHealth * 100f), 0, 100);
        var state = percent >= 80
            ? "health_high"
            : percent >= 40
                ? "health_medium"
                : "health_low";

        return (state, percent);
    }

    private int GetFollowerCount(EntityUid uid)
    {
        return TryComp<FollowedComponent>(uid, out var followed)
            ? followed.Following.Count
            : 0;
    }

    private void WarpTo(EntityUid uid, EntityUid target)
    {
        _adminLog.Add(LogType.GhostWarp, $"{ToPrettyString(uid)} RMC ghost warped to {ToPrettyString(target)}");

        if ((TryComp(target, out WarpPointComponent? warp) && warp.Follow) ||
            HasComp<MobStateComponent>(target))
        {
            _followerSystem.StartFollowingEntity(uid, target);
            return;
        }

        var xform = Transform(uid);
        _transformSystem.SetCoordinates(uid, xform, Transform(target).Coordinates);
        _transformSystem.AttachToGridOrMap(uid, xform);
        if (_physicsQuery.TryComp(uid, out var physics))
            _physics.SetLinearVelocity(uid, Vector2.Zero, body: physics);
    }
}
