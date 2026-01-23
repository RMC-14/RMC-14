using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Megaphone;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCMegaphoneSystem))]
public sealed partial class RMCMegaphoneComponent : Component
{
    /// <summary>
    /// Voice range for megaphone (normal voice range is 10).
    /// </summary>
    [DataField, AutoNetworkedField]
    public float VoiceRange = 15f;

    /// <summary>
    /// Current radius in tiles within which hushed effect is applied.
    /// Must not exceed voice range and is configured in 5-tile steps.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float HushedEffectRange = 15f;

    /// <summary>
    /// Sound played when toggling amplifying mode.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? ToggleSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/click.ogg");

    /// <summary>
    /// Duration of RMCStatusEffectHushed applied to recipients when user has leadership skill level 1+ and amplifying is enabled.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan HushedEffectDuration = TimeSpan.FromSeconds(4);
}
