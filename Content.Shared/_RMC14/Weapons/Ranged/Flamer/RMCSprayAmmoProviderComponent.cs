using Content.Shared.FixedPoint;
using Content.Shared.Weapons.Ranged;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Weapons.Ranged.Flamer;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCFlamerSystem))]
public sealed partial class RMCSprayAmmoProviderComponent : Component, IShootable
{
    [DataField, AutoNetworkedField]
    public string SolutionId = "spray";

    [DataField, AutoNetworkedField]
    public FixedPoint2 Cost = FixedPoint2.New(5);

    [DataField, AutoNetworkedField]
    public EntProtoId Spawn = "RMCExtinguisherSpray";

    [DataField, AutoNetworkedField]
    public bool HitUser = true;
}
