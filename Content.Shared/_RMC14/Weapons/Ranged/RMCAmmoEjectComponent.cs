using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Weapons.Ranged;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMGunSystem))]
public sealed partial class RMCAmmoEjectComponent : Component
{
    /// <summary>
    /// The ID of the container from which ammo should be ejected.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string ContainerID = "gun_magazine";

    /// <summary>
    /// This is the sound that will play if the container ID does not match an item slot on the weapon.
    /// Otherwise, the item slot's eject sound will play instead.
    /// This field is mainly for weapons that use BallisticAmmoProviderComponent to store their ammo, like grenade launchers and pump-action shotguns.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier EjectSound = new SoundPathSpecifier("/Audio/_RMC14/Weapons/Guns/Reload/m41_unload.ogg", AudioParams.Default.WithVolume(-2f));
}
