using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Megaphone;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCMegaphoneSystem))]
public sealed partial class RMCMegaphoneComponent : Component
{
    /// <summary>
    /// Voice range multiplier for megaphone (normal voice range is 10).
    /// </summary>
    [DataField, AutoNetworkedField]
    public float VoiceRange = 15f;

    /// <summary>
    /// Whether the megaphone amplifies voice range. Can be toggled via verb.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Amplifying = true;

    /// <summary>
    /// Sound played when toggling amplifying mode.
    /// </summary>
    [DataField]
    public SoundSpecifier? ToggleSound = new SoundPathSpecifier("/Audio/Machines/switch.ogg");
}
