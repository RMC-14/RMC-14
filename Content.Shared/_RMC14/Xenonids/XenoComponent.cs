using System.Linq;
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
public sealed partial class XenoComponent : Component, IComponentDebug
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

    public EmoteSoundsPrototype? Sounds;

    [DataField, AutoNetworkedField]
    public VisibilityFlags Visibility = VisibilityFlags.Xeno;

    [DataField, AutoNetworkedField]
    public XenoPheromones? IgnorePheromones;

    public string GetDebugString()
    {
        return $"""
            Role: {Role.Id}
            ActionIds:
              {string.Join("\r\n  ", ActionIds.Order())}
            AccessLevels:
              {string.Join("\r\n  ", AccessLevels.Order())}
            Tier: {Tier}
            HudOffset: {HudOffset}
            ContributesToVictory: {ContributesToVictory}
            CountedInSlots: {CountedInSlots}
            BypassTierCount: {BypassTierCount}
            ArmorAlert: {ArmorAlert.Id}
            SpawnAtLeaderPoint: {SpawnAtLeaderPoint}
            EmoteSounds: {EmoteSounds?.Id}
            Sounds: {Sounds?.ID}
            MuteOnSpawn: {MuteOnSpawn}
            Visibility: {Visibility}
            IgnorePheromones: {IgnorePheromones}
            """;
    }
}
