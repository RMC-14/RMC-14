using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.TailTrip;

[RegisterComponent, NetworkedComponent]
[Access(typeof(XenoTailTripSystem))]
public sealed partial class TailTripSlowedComponent : Component
{
    [DataField]
    public TimeSpan ExpiresAt;

    [DataField]
    public FixedPoint2 SpeedMult = FixedPoint2.New(0.66);
}
