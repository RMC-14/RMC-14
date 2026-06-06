using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Despoiler;

[RegisterComponent]
public sealed partial class XenoDespoilerOozingWoundsActionComponent : Component
{
    [DataField]
    public int BaseRadius = 1;

    [DataField]
    public float SeverityHpThreshold1 = 0.7f;

    [DataField]
    public float SeverityHpThreshold2 = 0.3f;

    [DataField]
    public float LingeringAcidChance = 0.2f;

    [DataField]
    public TimeSpan DistanceDelayPerTile = TimeSpan.FromSeconds(0.2);

    [DataField]
    public EntProtoId TelegraphProto = "RMCEffectDespoilerOozingTelegraph";

    [DataField]
    public EntProtoId AcidSprayProto = "RMCEffectDespoilerAcidSpray";

    [DataField]
    public EntProtoId AcidSprayEmpoweredProto = "RMCEffectDespoilerAcidSprayEmpowered";

    [DataField]
    public EntProtoId LingeringAcidProto = "RMCEffectDespoilerLingeringAcid";

    [DataField]
    public SoundSpecifier? CastSound;
}
