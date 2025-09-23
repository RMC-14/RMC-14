using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Weapons.Ranged.Ammo.BulletBox;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(BulletBoxSystem))]
public sealed partial class RefillableByBulletBoxComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public EntProtoId? BulletType;
}
