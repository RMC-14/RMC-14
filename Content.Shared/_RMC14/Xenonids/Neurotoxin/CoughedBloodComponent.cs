using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.GasToggle;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CoughedBloodComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 SlowMultiplier = 0.87f;

    [DataField, AutoNetworkedField]
    public TimeSpan ExpireTime;
}
