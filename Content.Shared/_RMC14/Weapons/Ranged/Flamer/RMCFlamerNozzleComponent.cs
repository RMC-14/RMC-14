using Content.Shared._RMC14.Maths;
using Content.Shared.FixedPoint;
using Content.Shared.Weapons.Ranged;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Weapons.Ranged.Flamer;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCFlamerSystem))]
public sealed partial class RMCFlamerNozzleComponent : Component, IShootable
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 CostPer = FixedPoint2.New(3);

    [DataField, AutoNetworkedField]
    public float MaxRange = RMCMathExtensions.CircleAreaFromSquareAbilityRange(5);

    [DataField, AutoNetworkedField]
    public EntProtoId Projectile = "RMCBulletSentryFireProjectile";
}
