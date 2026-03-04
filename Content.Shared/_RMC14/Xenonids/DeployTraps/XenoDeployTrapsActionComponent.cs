using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.DeployTraps;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoDeployTrapsSystem))]
public sealed partial class XenoDeployTrapsActionComponent : Component
{
    [DataField, AutoNetworkedField]
    public float FailCooldownMult = 0.5f;
}
