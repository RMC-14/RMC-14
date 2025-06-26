using Content.Shared._RMC14.Tracker.SquadLeader;
using Content.Shared.Access;
using Content.Shared.Radio;
using Content.Shared.Roles;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Marines.Squads;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SquadSystem), Other = AccessPermissions.Read)]
[EntityCategory("Squads")]
public sealed partial class SquadTeamComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool RoundStart;

    [DataField(required: true), AutoNetworkedField]
    public Color Color;

    [DataField(required: true), AutoNetworkedField]
    public ProtoId<RadioChannelPrototype>? Radio;

    [DataField(required: true), AutoNetworkedField]
    public SpriteSpecifier Background;

    [DataField, AutoNetworkedField]
    public SpriteSpecifier.Rsi? MinimapBackground;

    [DataField, AutoNetworkedField]
    public ProtoId<AccessLevelPrototype>[] AccessLevels = Array.Empty<ProtoId<AccessLevelPrototype>>();

    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> Members = new();

    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<JobPrototype>, int> Roles = new();

    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<JobPrototype>, int> MaxRoles = new();

    [DataField, AutoNetworkedField]
    public bool CanSupplyDrop = true;

    [DataField, AutoNetworkedField]
    public List<SquadArmorLayers> BlacklistedSquadArmor = new();

    [DataField, AutoNetworkedField]
    [Access(typeof(SquadLeaderTrackerSystem))]
    public FireteamData Fireteams = new();

    [DataField, AutoNetworkedField]
    public string Group = "UNMC";

    [DataField, AutoNetworkedField]
    public SpriteSpecifier.Rsi LeaderIcon = new(new ResPath("_RMC14/Interface/cm_job_icons.rsi"), "hudsquad_leader_a");
}
