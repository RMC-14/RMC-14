using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Megaphone;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCMegaphoneSystem))]
public sealed partial class RMCMegaphoneComponent : Component
{
    /// <summary>
    /// Multiplier applied to the base voice range (normal voice range is 10).
    /// </summary>
    [DataField, AutoNetworkedField]
    public float VoiceRangeMultiplier = 1.5f;

    /// <summary>
    /// Maximum radius in tiles within which hushed effect can be applied.
    /// Configured in 5-tile steps.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaxHushedEffectRange = 15f;

    /// <summary>
    /// Current radius in tiles within which hushed effect is applied.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float CurrentHushedEffectRange = 15f;

    /// <summary>
    /// Sound played when toggling CurrentHushedEffectRange.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? ToggleSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/click.ogg");

    /// <summary>
    /// Duration of RMCStatusEffectHushed applied to recipients.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan HushedEffectDuration = TimeSpan.FromSeconds(5);
}
