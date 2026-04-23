using Content.Server._RMC14.Mentor.ImaginaryFriend;
using Content.Shared.Ghost;
using Content.Shared.Verbs;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Mentor;

public sealed partial class MentorVerbSystem : EntitySystem
{
    [Dependency] private readonly ImaginaryFriendSystem _imaginaryFriend = default!;
    [Dependency] private readonly MentorManager _mentorManager = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GetVerbsEvent<Verb>>(AddMentorVerbs);
    }

    private void AddMentorVerbs(GetVerbsEvent<Verb> args)
    {
        var user = args.User;

        if (!HasComp<GhostComponent>(user))
            return;

        if (!TryComp(user, out ActorComponent? actor))
            return;

        var session = actor.PlayerSession;

        if (!_mentorManager.IsMentor(session.UserId))
            return;

        args.Verbs.Add(new()
        {
            Act = () => _imaginaryFriend.OpenImaginaryFriendConfirmWindow(session, args.Target),
            Text = Loc.GetString("rmc-mentor-imaginary-friend-verb"),
            Priority = -25,
            Icon = new SpriteSpecifier.Rsi(new ResPath("Mobs/Ghosts/ghost_human.rsi"), "icon"),
        });
    }
}
