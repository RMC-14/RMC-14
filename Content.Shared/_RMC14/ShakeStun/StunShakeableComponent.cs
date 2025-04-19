using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.ShakeStun;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(StunShakeableSystem))]
public sealed partial class StunShakeableComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan DurationRemoved = TimeSpan.FromSeconds(6);

    /// <summary>
    /// Sound to play when the player is shaked.
    /// </summary>
    [DataField]
    public SoundSpecifier ShakeSound = new SoundPathSpecifier("/Audio/Effects/thudswoosh.ogg")
    {
        Params = AudioParams.Default.WithVariation(0.05f)
    };
}
