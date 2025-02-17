using Content.Shared._RMC14.Dropship.Utility.Systems;
using Content.Shared._RMC14.Marines.Skills;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Dropship.Utility.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCFultonSystem))]
public sealed partial class RMCCanBeFultonedComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan Delay = TimeSpan.FromSeconds(5);

    [DataField, AutoNetworkedField]
    public EntProtoId<SkillDefinitionComponent> Skill = "RMCSkillIntel";

    [DataField, AutoNetworkedField]
    public TimeSpan ReturnDelay = TimeSpan.FromSeconds(150);

    [DataField, AutoNetworkedField]
    public SoundSpecifier? FultonSound = new SoundPathSpecifier("/Audio/_RMC14/Items/fulton.ogg");
}
