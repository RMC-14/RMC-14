using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Weapons.Ranged;


[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMGunSystem))]
public sealed partial class AssistedReloadReceiverComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Weapon;
}