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
    /// Whether the hushed effect is applied to recipients. Can be toggled via verb.
    /// Voice range extension always works regardless of this setting.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Amplifying = true;

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
