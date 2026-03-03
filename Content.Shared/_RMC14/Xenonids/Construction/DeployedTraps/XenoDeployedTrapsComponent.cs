using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Construction.DeployedTraps;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoDeployedTrapsComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan StunDuration = TimeSpan.FromSeconds(1.75);
}
