using Content.Server.Mind;
using Content.Server.Players.PlayTimeTracking;
using Content.Server.Roles;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.Roles.Jobs;
using Robust.Server.GameStates;
using Robust.Shared.Player;

namespace Content.Server._RMC14.Xenonids;

public sealed class XenoRoleSystem : EntitySystem
{
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly PlayTimeTrackingSystem _playTime = default!;
    [Dependency] private readonly PvsOverrideSystem _pvsOverride = default!;
    [Dependency] private readonly RoleSystem _role = default!;

    private EntityQuery<ActorComponent> _actorQuery;

    public override void Initialize()
    {
        base.Initialize();

        _actorQuery = GetEntityQuery<ActorComponent>();

        SubscribeLocalEvent<XenoComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<XenoComponent, PlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<XenoComponent, HiveChangedEvent>(OnHiveChanged);
    }

    private void OnPlayerAttached(Entity<XenoComponent> xeno, ref PlayerAttachedEvent args)
    {
        if (!_mind.TryGetMind(args.Player.UserId, out var mind))
            return;

        // TODO: GetHive
        if (xeno.Comp.Hive is {} hive)
            _pvsOverride.AddForceSend(hive, args.Player);

        _role.MindTryRemoveRole<JobComponent>(mind.Value);
        _role.MindAddRole(mind.Value, new JobComponent { Prototype = xeno.Comp.Role });
        _playTime.PlayerRolesChanged(args.Player);
    }

    private void OnPlayerDetached(Entity<XenoComponent> xeno, ref PlayerDetachedEvent args)
    {
        // TODO: GetHive
        if (xeno.Comp.Hive is {} hive)
            _pvsOverride.RemoveForceSend(hive, args.Player);
    }

    private void OnHiveChanged(Entity<XenoComponent> xeno, ref HiveChangedEvent args)
    {
        if (_actorQuery.CompOrNull(xeno)?.PlayerSession is not {} session)
            return;

        if (args.OldHive is {} oldHive)
            _pvsOverride.RemoveForceSend(oldHive, session);

        if (args.Hive is {} newHive)
            _pvsOverride.AddForceSend(newHive, session);
    }
}
