using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Projectile;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]

public sealed partial class DrainOnHitComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 DrainAmount = FixedPoint2.New(2);

    [DataField]
    public string TargetSolution = "chemicals";

    [DataField]
    public string DrainGroup = "Medicine";
}
