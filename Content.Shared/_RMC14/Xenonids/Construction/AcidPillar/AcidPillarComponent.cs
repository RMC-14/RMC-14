using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Xenonids.Construction.AcidPillar;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(AcidPillarSystem))]
public sealed partial class AcidPillarComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan CheckEvery = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan NextCheck;

    [DataField, AutoNetworkedField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan Next;

    [DataField, AutoNetworkedField]
    public EntProtoId Acid = "XenoAcidSprayWeak";

    [DataField, AutoNetworkedField]
    public TimeSpan AcidSpreadDelay = TimeSpan.FromSeconds(0.3);

    [DataField, AutoNetworkedField]
    public float Range = 5;

    [DataField, AutoNetworkedField]
    public SpriteSpecifier.Rsi? IdleSprite = new(new ResPath("_RMC14/Structures/Xenos/xeno_acid_pillar.rsi"), "idle");

    [DataField, AutoNetworkedField]
    public SpriteSpecifier.Rsi? FiringSprite = new(new ResPath("_RMC14/Structures/Xenos/xeno_acid_pillar.rsi"), "firing");
}
