using Content.Server.Mind;
using Content.Server.Players.PlayTimeTracking;
using Content.Server.Roles;
using Content.Shared._CM14.Xenonids;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Player;

namespace Content.Server._CM14.Xenonids;

public sealed class XenoRoleSystem : EntitySystem
{
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly PlayTimeTrackingSystem _playTime = default!;
    [Dependency] private readonly RoleSystem _role = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoComponent, PlayerAttachedEvent>(OnPlayerAttached);
    }

    private void OnPlayerAttached(Entity<XenoComponent> xeno, ref PlayerAttachedEvent args)
    {
        if (!_mind.TryGetMind(args.Player.UserId, out var mind))
            return;

        _role.MindTryRemoveRole<JobComponent>(mind.Value);
        _role.MindAddRole(mind.Value, new JobComponent { Prototype = xeno.Comp.Role });
        _playTime.PlayerRolesChanged(args.Player);
    }
}
