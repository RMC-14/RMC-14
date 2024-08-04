using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Weapons.Ranged;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedPumpActionSystem))]
public sealed partial class RMCAmmoEjectComponent : Component
{
    [DataField, AutoNetworkedField]
    public string ContainerID = "gun_magazine";

    [DataField, AutoNetworkedField]
    public SoundSpecifier EjectSound = new SoundPathSpecifier("/Audio/_RMC14/Weapons/Guns/Reload/m41_unload.ogg", AudioParams.Default.WithVolume(-2f));
}
