using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Medical.Surgery.Effects.Step;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedCMSurgerySystem))]
public sealed partial class RMCSurgeryStepLarvaEffectComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId DeadLarvaItem = "RMCXenoEmbryo";
}
