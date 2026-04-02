namespace Content.Shared._RMC14.Xenonids.HiveTeam;

[RegisterComponent]
public sealed partial class HiveTeamsComponent : Component
{
    public const int TeamCount = 4;
    public static readonly string[] RoleNames = ["Combat: Maim", "Combat: Rip", "Combat: Tear", "Follow Queen", "Build", "Backline", "Capture", "Stall"];

    public List<HiveTeamEntry> Teams = [];
}

public sealed class HiveTeamEntry
{
    public EntityUid? Leader;
    public List<EntityUid> Members = [];
    public int Role = 0;
}
