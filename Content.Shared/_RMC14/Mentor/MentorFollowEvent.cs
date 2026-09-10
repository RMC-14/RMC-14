namespace Content.Shared._RMC14.Mentor;

[ByRefEvent]
public record struct MentorFollowEvent(NetEntity Follower, NetEntity Target);
