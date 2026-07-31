using Content.Shared._RMC14.Mentor;
using Content.Shared._RMC14.Mobs;
using Content.Shared.Follower;

namespace Content.Server._RMC14.Mentor;

public sealed class MentorSystem : EntitySystem
{
    [Dependency] private readonly FollowerSystem _follower = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<CMGhostComponent, MentorFollowEvent>(HandleMentorFollow);
    }

    private void HandleMentorFollow(Entity<CMGhostComponent> ent, ref MentorFollowEvent args)
    {
        _follower.StartFollowingEntity(GetEntity(args.Follower), GetEntity(args.Target));
    }
}
