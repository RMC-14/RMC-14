using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Punch;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]

public sealed partial class PunchSlowedComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 SlowAmount = FixedPoint2.New(0.66);

    [DataField, AutoNetworkedField]
    public TimeSpan ExpiresAt;
}
