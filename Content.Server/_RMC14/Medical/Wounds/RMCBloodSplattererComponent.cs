using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Medical.Wounds;

[RegisterComponent]
[Access(typeof(RMCBloodSplatterSystem))]
public sealed partial class RMCBloodSplattererComponent : Component
{
    [DataField]
    public EntProtoId BloodDecal = new("RMCDecalSpawnerBloodSplash");

    [DataField]
    public EntProtoId BloodMinorDecal = new ("RMCDecalSpawnerBloodDrip");

    [DataField]
    public EntProtoId VomitDecal = new("RMCDecalSpawnerVomitSplash");

    [DataField]
    public FixedPoint2 MinimalTriggerDamage = 10;

}
