using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Cleave;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CleaveRootedComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan ExpiresAt;
}
