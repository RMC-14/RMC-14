using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Construction.DeployedTraps;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]

public sealed partial class XenoCaughtInTrapComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan ExpireTime;
}
