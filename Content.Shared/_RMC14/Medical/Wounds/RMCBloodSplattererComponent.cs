using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Medical.Wounds;

[RegisterComponent, NetworkedComponent]
public sealed partial class RMCBloodSplattererComponent : Component
{
    [DataField]
    public EntProtoId BloodDecal = new("RMCBloodDecal");

    [DataField]
    public EntProtoId BloodMinorDecal = new ("RMCBloodMinorDecal");

    [DataField]
    public EntProtoId VomitDecal = new("RMCVomitDecal");

    [DataField]
    public FixedPoint2 MinimalTriggerDamage = 10;

}
