using Content.Shared._RMC14.Xenonids.Insight;
using Content.Shared.Chat.Prototypes;
using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Xenonids.DeployTraps;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoDeployTrapsSystem), typeof(XenoInsightSystem))]
public sealed partial class XenoDeployTrapsComponent : Component
{
    [DataField, AutoNetworkedField]
    public float DeployTrapsRadius = 2f;

    // Prototype for trap to create
    [DataField, AutoNetworkedField]
    public EntProtoId DeployTrapsId = "XenoTrapperTrap";

    [DataField, AutoNetworkedField]
    public ProtoId<EmotePrototype>? Emote = "XenoRoar";

    // Prototype for trap to create
    [DataField, AutoNetworkedField]
    public EntProtoId DeployEmpoweredTrapsId = "XenoTrapperEmpoweredTrap";

    [DataField, AutoNetworkedField]
    public SoundSpecifier DeploySound = new SoundCollectionSpecifier("XenoResinBreak");

    [DataField, AutoNetworkedField]
    public SpriteSpecifier.Rsi ActionIcon = new(new ResPath("_RMC14/Actions/xeno_actions.rsi"), "gas_mine");

    [DataField, AutoNetworkedField]
    public SpriteSpecifier.Rsi ActionIconEmpowered = new(new ResPath("_RMC14/Actions/xeno_actions.rsi"), "gas_mine_empowered");

    [DataField, AutoNetworkedField]
    public int Range = 13;

    [DataField, AutoNetworkedField]
    public bool Empowered = false;
}
