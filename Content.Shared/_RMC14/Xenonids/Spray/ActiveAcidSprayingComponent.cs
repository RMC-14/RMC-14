using Content.Shared._RMC14.Line;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Spray;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoSprayAcidSystem))]
public sealed partial class ActiveAcidSprayingComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId Acid;

    [DataField, AutoNetworkedField]
    public List<LineTile> Spawn = new();

    [DataField, AutoNetworkedField]
    public EntityUid? Blocker;

    [DataField, AutoNetworkedField]
    public EntityUid? Chain;
}
