using Content.Shared._RMC14.Xenonids.Despoiler.Lingering;
using Content.Shared._RMC14.Xenonids.Despoiler.Spray;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Spawners;

namespace Content.Shared._RMC14.Xenonids.Despoiler.OozingWounds;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoDespoilerOozingWoundsActionComponent : Component
{
    [DataField, AutoNetworkedField]
    public int BaseRadius = 1;

    [DataField, AutoNetworkedField]
    public float SeverityHpThreshold1 = 0.7f;

    [DataField, AutoNetworkedField]
    public float SeverityHpThreshold2 = 0.3f;

    [DataField, AutoNetworkedField]
    public float LingeringAcidChance = 0.2f;

    [DataField, AutoNetworkedField]
    public TimeSpan DistanceDelayPerTile = TimeSpan.FromSeconds(0.2);

    [DataField, AutoNetworkedField]
    public EntProtoId<TimedDespawnComponent> TelegraphProto = "RMCEffectDespoilerOozingTelegraph";

    [DataField, AutoNetworkedField]
    public EntProtoId<XenoDespoilerAcidSprayComponent> AcidSprayProto = "RMCEffectDespoilerAcidSpray";

    [DataField, AutoNetworkedField]
    public EntProtoId<XenoDespoilerAcidSprayComponent> AcidSprayEmpoweredProto = "RMCEffectDespoilerAcidSprayEmpowered";

    [DataField, AutoNetworkedField]
    public EntProtoId<XenoDespoilerLingeringAcidComponent> LingeringAcidProto = "RMCEffectDespoilerLingeringAcid";

    [DataField, AutoNetworkedField]
    public SoundSpecifier? CastSound;
}
