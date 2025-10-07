using Content.Shared._RMC14.Attachable.Systems;
using Robust.Shared.GameStates;
//using Content.Shared.FixedPoint;
using Content.Shared.Weapons.Ranged;
//using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared._RMC14.Weapons.Ranged.Flamer;
using Content.Shared.FixedPoint;
//using Content.Shared.Weapons.Ranged;
//using Robust.Shared.GameStates;
//using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Attachable;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(AttachableSharedTankSystem))]
public sealed partial class RMCAttachableSharedTankComponent : Component, IShootable
{


    [DataField, AutoNetworkedField]
    public EntityUid Holder;

    //[DataField, AutoNetworkedField]
    //public List<EntProtoId> Spawn = ["RMCBulletFireVesgRed", "RMCBulletFireVesgGreen", "RMCBulletFireVesgBlue"]; // The projectiles

    [DataField, AutoNetworkedField]
    public bool Enabled; // This should be the same value of the flamer's Igniter

    [DataField, AutoNetworkedField]
    public FixedPoint2 CostPer = FixedPoint2.New(3);
}