using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Mobs;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCPulseComponent : Component
{
    /// <summary>
    /// The threshold above which pulse is considered thready and displayed as >250.
    /// A thready pulse is weak, fine, and barely perceptible, often feeling like a thin thread under the finger.
    /// </summary>
    public const int ThreadyPulseThreshold = 250;

    /// <summary>
    /// Blood volume percentage at or below which pulse becomes thready.
    /// Corresponds to BLOOD_VOLUME_BAD (224/560 ≈ 40%).
    /// </summary>
    public const float ThreadyBloodThreshold = 0.4f;

    /// <summary>
    /// The current pulse state of this entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public PulseState State = PulseState.Normal;
}

public enum PulseState : byte
{
    None = 0, // Dead or species without blood.
    Slow = 1, // 40-60 bpm
    Normal = 2, // 60-90 bpm
    Fast = 3, // 90-120 bpm
    VeryFast = 4, // 120-160 bpm
    /// <summary>
    /// Thready pulse - extremely weak and fast, displayed as ">250".
    /// Occurs when blood volume is critically low.
    /// </summary>
    Thready = 5
}
