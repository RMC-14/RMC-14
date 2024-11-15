using Content.Shared._RMC14.Marines.Skills;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Marines.Announce;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedMarineAnnounceSystem))]
public sealed partial class MarineCommunicationsComputerComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(30);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan? LastAnnouncement;

    [DataField, AutoNetworkedField]
    public EntProtoId<SkillDefinitionComponent> OverwatchSkill = "RMCSkillOverwatch";

    [DataField, AutoNetworkedField]
    public int OverwatchSkillLevel = 1;

    /*
    [DataField, AutoNetworkedField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/_RMC14/Announcements/Marine/notice2.ogg");
    */
}
