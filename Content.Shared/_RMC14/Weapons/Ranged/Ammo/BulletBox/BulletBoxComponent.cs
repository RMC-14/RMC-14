using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Weapons.Ranged.Ammo.BulletBox;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(BulletBoxSystem))]
public sealed partial class BulletBoxComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Amount = 600;

    [DataField, AutoNetworkedField]
    public int Max = 600;

    [DataField(required: true), AutoNetworkedField]
    public EntProtoId BulletType;

    [DataField, AutoNetworkedField]
    public TimeSpan Delay = TimeSpan.FromSeconds(1.5);
}
