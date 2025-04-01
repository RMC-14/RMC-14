using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Construction.RecoveryNode;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RecoveryNodeComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 HealAmount = 20;

    [DataField, AutoNetworkedField]
    public float HealRange = 1.5F;

    [DataField, AutoNetworkedField]
    public TimeSpan HealCooldown = TimeSpan.FromSeconds(5);

    [DataField, AutoNetworkedField]
    public TimeSpan NextHealAt;

    [DataField]
    public DoAfterId? HealDoafter;
}
