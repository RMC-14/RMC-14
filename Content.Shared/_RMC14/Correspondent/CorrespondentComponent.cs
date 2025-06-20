using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Correspondant;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CorrespondentComponent : Component
{
    /// <summary>
    ///     Stun time after failing to shoot a gun.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan GunShootFailStunTime = TimeSpan.FromSeconds(10);

    /// <summary>
    ///     Stun time after failing to shoot a gun.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan ShakeFailStunTime = TimeSpan.FromSeconds(10);

    /// <summary>
    ///     Noise to play after failing to shoot a gun. Boom!
    /// </summary>
    [DataField]
    public SoundSpecifier GunShootFailSound = new SoundPathSpecifier("/Audio/Weapons/Guns/Gunshots/bang.ogg");

    [DataField]
    public LocId GunFailedMessage = "correspondent-gun-clumsy";

    [DataField]
    public LocId ShakeFailedMessage = "correspondent-shake-failed";
}
