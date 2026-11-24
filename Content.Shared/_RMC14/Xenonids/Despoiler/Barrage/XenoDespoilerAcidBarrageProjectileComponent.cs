using System.Numerics;
using Content.Shared._RMC14.Xenonids.Despoiler.Lingering;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Despoiler.Barrage;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoDespoilerAcidBarrageProjectileComponent : Component
{
    [DataField, AutoNetworkedField]
    public float LingeringAcidChance = 0.25f;

    [DataField, AutoNetworkedField]
    public EntProtoId<XenoDespoilerLingeringAcidComponent> LingeringAcidProto = "RMCEffectDespoilerLingeringAcid";

    [DataField, AutoNetworkedField]
    public EntityUid? Shooter;

    [DataField, AutoNetworkedField]
    public Vector2 Scale = Vector2.One;
}
