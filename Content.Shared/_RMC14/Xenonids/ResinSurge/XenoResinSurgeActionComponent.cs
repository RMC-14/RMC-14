using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.ResinSurge;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedXenoResinSurgeSystem))]
public sealed partial class XenoResinSurgeActionComponent : Component
{
    [DataField, AutoNetworkedField]
    public float FailCooldownMult = 0.5f;

    [DataField, AutoNetworkedField]
    public TimeSpan SuccessCooldown = TimeSpan.FromSeconds(10);
}
