using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Weapons.Ranged.SmartGun;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SmartGunSystem))]
public sealed partial class SmartGunComponent : Component
{
    [DataField, AutoNetworkedField]
    public float IFFBatteryDrain = 0.4f;
}
