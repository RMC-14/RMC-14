using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Plasma;

[RegisterComponent]
public sealed partial class XenoRemoteTransferPlasmaComponent : Component
{
    public readonly FixedPoint2 PlasmaPercentage = 0.75;
}
