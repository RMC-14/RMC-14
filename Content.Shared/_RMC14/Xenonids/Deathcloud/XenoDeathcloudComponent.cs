using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Deathcloud;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoDeathcloudSystem))]
public sealed partial class XenoDeathcloudComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId Spawn = "RMCSmokeAcidDeathcloud";
}
