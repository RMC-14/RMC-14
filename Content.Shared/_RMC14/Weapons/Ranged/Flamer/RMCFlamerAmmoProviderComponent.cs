using Content.Shared.FixedPoint;
using Content.Shared.Weapons.Ranged;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Weapons.Ranged.Flamer;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCFlamerSystem))]
public sealed partial class RMCFlamerAmmoProviderComponent : Component, IShootable
{
    [DataField, AutoNetworkedField]
    public string ContainerId = "gun_magazine";

    [DataField, AutoNetworkedField]
    public int Range = 5;

    [DataField, AutoNetworkedField]
    public TimeSpan DelayPer = TimeSpan.FromSeconds(0.05);

    [DataField, AutoNetworkedField]
    public FixedPoint2 CostPer = FixedPoint2.New(1);

    [DataField, AutoNetworkedField]
    public EntProtoId Spawn = "RMCTileFire";

    [DataField, AutoNetworkedField]
    public int MaxIntensity = 20;

    [DataField, AutoNetworkedField]
    public int MaxDuration = 24;
}
