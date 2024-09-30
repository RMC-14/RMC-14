using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.ResinSurge;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedXenoResinSurgeSystem))]
public sealed partial class XenoResinSurgeActionComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan FailCooldown = TimeSpan.FromSeconds(5);
}
