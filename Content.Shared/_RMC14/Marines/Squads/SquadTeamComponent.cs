using Content.Shared._RMC14.Tracker.SquadLeader;
using Content.Shared.Access;
using Content.Shared.Radio;
using Content.Shared.Roles;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Marines.Squads;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SquadSystem))]
[EntityCategory("Squads")]
public sealed partial class SquadTeamComponent : Component
{
    [DataField]
    public bool RoundStart;

    [DataField(required: true)]
    public Color Color;

    [DataField(required: true)]
    public ProtoId<RadioChannelPrototype>? Radio;

    [DataField(required: true)]
    public SpriteSpecifier Background;

    [DataField]
    public SpriteSpecifier.Rsi? MinimapBackground;

    [DataField]
    public ProtoId<AccessLevelPrototype>[] AccessLevels = Array.Empty<ProtoId<AccessLevelPrototype>>();

    [DataField]
    public HashSet<EntityUid> Members = new();

    [DataField]
    public Dictionary<ProtoId<JobPrototype>, int> Roles = new();

    [DataField]
    public Dictionary<ProtoId<JobPrototype>, int> MaxRoles = new();

    [DataField]
    public bool CanSupplyDrop = true;

    [DataField]
    public List<SquadArmorLayers> BlacklistedSquadArmor = new();

    [DataField]
    [Access(typeof(SquadLeaderTrackerSystem))]
    public FireteamData Fireteams = new();

    [DataField]
    public string Group = "UNMC";

    [DataField]
    public SpriteSpecifier.Rsi LeaderIcon = new(new ResPath("_RMC14/Interface/cm_job_icons.rsi"), "hudsquad_leader_a");
}
