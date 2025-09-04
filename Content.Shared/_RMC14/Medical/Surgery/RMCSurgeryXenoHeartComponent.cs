using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Medical.Surgery;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedCMSurgerySystem))]
public sealed partial class RMCSurgeryXenoHeartComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId Item = "RMCOrganXenoHeartT1";
}
