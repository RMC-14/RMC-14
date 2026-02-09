using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Mobs;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCPulseComponent : Component
{
    /// <summary>
    /// The current pulse state of this entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public PulseState State = PulseState.Normal;

    /// <summary>
    /// The last calculated pulse value in bpm.
    /// For thready pulse, this will be 250+.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int LastPulseValue;
}

public enum PulseState : byte
{
    None = 0, // Dead or species without blood.
    Slow = 1, // 40-60 bpm
    Normal = 2, // 60-90 bpm
    Fast = 3, // 90-120 bpm
    VeryFast = 4, // 120-160 bpm

    /// <summary>
    /// Thready pulse - extremely weak and fast, >250 bpm.
    /// Occurs when blood volume is critically low.
    /// </summary>
    Thready = 5
}
