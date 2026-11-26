using Content.Shared._RMC14.Announce;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.NPC.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Marines.Announce;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
[Access(typeof(SharedMarineAnnounceSystem))]
public sealed partial class MarineCommunicationsComputerComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(30);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan? LastAnnouncement;

    [DataField, AutoNetworkedField]
    public EntProtoId<SkillDefinitionComponent> AnnounceSkill = "RMCSkillLeadership";

    [DataField, AutoNetworkedField]
    public int AnnounceSkillLevel = 1;

    [DataField, AutoNetworkedField]
    public EntProtoId<SkillDefinitionComponent> OverwatchSkill = "RMCSkillOverwatch";

    [DataField, AutoNetworkedField]
    public int OverwatchSkillLevel = 1;

    [DataField, AutoNetworkedField]
    public bool CanCreateEcho = true;

    [DataField, AutoNetworkedField]
    public bool CanGiveMedals;

    [DataField, AutoNetworkedField]
    public bool CanInitiateEvac;

    [DataField]
    public EntProtoId<RMCAnnouncementPresetComponent>? AnnouncementOverlay = "RMCAnnouncementMarineCommand";

    [DataField, AutoNetworkedField]
    public string? AnnounceName;

    [DataField, AutoNetworkedField]
    public LocId? AnnounceAuthor;

    [DataField, AutoNetworkedField]
    public List<ProtoId<NpcFactionPrototype>> AnnounceFactions = new();

    [DataField, AutoNetworkedField]
    public bool CanDesignateLZ = true;

    [DataField, AutoNetworkedField]
    public string Planet = string.Empty;

    [DataField, AutoNetworkedField]
    public string Operation = string.Empty;

    [DataField, AutoNetworkedField]
    public List<LandingZone> LandingZones = new();

    /*
    [DataField, AutoNetworkedField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/_RMC14/Announcements/Marine/notice2.ogg");
    */
}
