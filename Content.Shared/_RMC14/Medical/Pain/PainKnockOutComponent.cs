using Robust.Shared.GameStates;
using Content.Shared.FixedPoint;

namespace Content.Shared._RMC14.Medical.Pain;

[RegisterComponent, NetworkedComponent]
public sealed partial class PainKnockOutComponent : Component
{
    public FixedPoint2 PreviousAliveThreshold;
    public FixedPoint2 PreviousCritThreshold;
    public bool IsAlreadySaved = false;
}
