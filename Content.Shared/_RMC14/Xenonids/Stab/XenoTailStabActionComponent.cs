using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Stab;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedXenoTailStabSystem))]
public sealed partial class XenoTailStabActionComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan MissCooldown = TimeSpan.FromSeconds(1);
}
