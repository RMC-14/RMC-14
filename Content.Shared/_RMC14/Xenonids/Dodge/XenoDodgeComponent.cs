using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Dodge;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoDodgeSystem))]
public sealed partial class XenoDodgeComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan Duration = TimeSpan.FromSeconds(7);

    [DataField, AutoNetworkedField]
    public float RefundMultiplier = 2f;

    [DataField, AutoNetworkedField]
    public TimeSpan ToggleLockoutTime = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public TimeSpan MinimumCooldown = TimeSpan.FromSeconds(5);
}
