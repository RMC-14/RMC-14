using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Weapons.Ranged.Battery;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCGunBatterySystem))]
public sealed partial class GunDrainBatteryOnShootComponent : Component
{
    [DataField, AutoNetworkedField]
    public float BaseDrain = 0.25f;

    [DataField, AutoNetworkedField]
    public float Drain = 0.25f;

    [DataField, AutoNetworkedField]
    public string BatteryContainer = "cell_slot";

    [DataField, AutoNetworkedField]
    public bool Powered;
}
