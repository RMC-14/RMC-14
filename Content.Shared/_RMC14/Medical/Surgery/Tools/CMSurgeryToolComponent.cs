using Content.Shared._RMC14.Marines.Skills;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Medical.Surgery.Tools;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedCMSurgerySystem))]
public sealed partial class CMSurgeryToolComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId<SkillDefinitionComponent> SkillType = "RMCSkillSurgery";

    [DataField, AutoNetworkedField]
    public int Skill = 1;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? StartSound;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? EndSound;
}
