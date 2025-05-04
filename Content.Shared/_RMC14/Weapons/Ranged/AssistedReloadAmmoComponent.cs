using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Weapons.Ranged;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMGunSystem))]
public sealed partial class AssistedReloadAmmoComponent : Component
{
    [DataField, AutoNetworkedField]
    public double InsertDelay = 3.0;
}