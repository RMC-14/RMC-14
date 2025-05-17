using System.Numerics;
using Content.Shared._RMC14.Xenonids.Pheromones;
using Content.Shared.Access;
using Content.Shared.Alert;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Eye;
using Content.Shared.Roles;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoSystem))]
public sealed partial class XenoComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public ProtoId<JobPrototype> Role;

    [DataField, AutoNetworkedField]
    public List<EntProtoId> ActionIds = new();

    [DataField, AutoNetworkedField]
    public Dictionary<EntProtoId, EntityUid> Actions = new();

    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<AccessLevelPrototype>> AccessLevels = new() { "CMAccessXeno" };

    [DataField, AutoNetworkedField]
    public int Tier;

    [DataField, AutoNetworkedField]
    public Vector2 HudOffset;

    [DataField, AutoNetworkedField]
    public bool ContributesToVictory = true;

    [DataField, AutoNetworkedField]
    public bool CountedInSlots = true;

    [DataField, AutoNetworkedField]
    public bool BypassTierCount;

    [DataField, AutoNetworkedField]
    public TimeSpan UnlockAt = TimeSpan.FromSeconds(60);

    [DataField, AutoNetworkedField]
    public ProtoId<AlertPrototype> ArmorAlert = "XenoArmor";

    [DataField, AutoNetworkedField]
    public bool SpawnAtLeaderPoint;

    [DataField, AutoNetworkedField]
    public ProtoId<EmoteSoundsPrototype>? EmoteSounds = "Xeno";

    [DataField, AutoNetworkedField]
    public bool MuteOnSpawn;

    /// <summary>
    /// Hides this xeno from the caste unlock announcements.
    /// Use for admeme or unimplemented castes that can't be evolved to.
    /// </summary>
    [DataField]
    public bool Hidden;

    public EmoteSoundsPrototype? Sounds;

    [DataField, AutoNetworkedField]
    public VisibilityFlags Visibility = VisibilityFlags.Xeno;

    [DataField, AutoNetworkedField]
    public XenoPheromones? IgnorePheromones;
}
