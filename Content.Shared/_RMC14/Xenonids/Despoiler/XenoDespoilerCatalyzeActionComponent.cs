using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Spawners;

namespace Content.Shared._RMC14.Xenonids.Despoiler;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoDespoilerCatalyzeActionComponent : Component
{
    [DataField, AutoNetworkedField]
    public int HypertensionCost = 1;

    [DataField, AutoNetworkedField]
    public TimeSpan BuffDuration = TimeSpan.FromSeconds(10);

    [DataField, AutoNetworkedField]
    public EntProtoId<TimedDespawnComponent> VisualProto = "RMCEffectDespoilerCatalyze";
}
