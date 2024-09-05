using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.GasToggle;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CoughedBloodComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 SlowMultiplier = FixedPoint2.New(0.87);

    [DataField, AutoNetworkedField]
    public TimeSpan ExpireTime;
}
