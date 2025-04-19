using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Dodge;

[RegisterComponent, NetworkedComponent]
[Access(typeof(XenoDodgeSystem))]
public sealed partial class XenoActiveDodgeComponent : Component
{
    [DataField]
    public FixedPoint2 SpeedMult = FixedPoint2.New(0.25);

    [DataField]
    public FixedPoint2 CrowdSpeedAddMult = FixedPoint2.New(0.25);

    [DataField]
    public float CrowdRange = 1.5f;

    [DataField]
    public TimeSpan ExpiresAt;

    [DataField]
    public bool InCrowd = false;
}
