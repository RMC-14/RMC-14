using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Mobs.Pulse;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCPulseComponent : Component
{
    /// <summary>
    /// The current pulse state of this entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public PulseState State = PulseState.Normal;

    /// <summary>
    /// The last calculated pulse value in BPM.
    /// For thready pulse, this will be 250+.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int LastPulseValue;
}

public enum PulseState : byte
{
    /// <summary>
    /// No pulse - dead or species without blood.
    /// </summary>
    None = 0,

    /// <summary>
    /// Slow pulse - 40-60 BPM.
    /// </summary>
    Slow = 1,

    /// <summary>
    /// Normal pulse - 60-90 BPM.
    /// </summary>
    Normal = 2,

    /// <summary>
    /// Fast pulse - 90-120 BPM.
    /// </summary>
    Fast = 3,

    /// <summary>
    /// Very fast pulse - 120-160 BPM.
    /// </summary>
    VeryFast = 4,

    /// <summary>
    /// Thready pulse - extremely weak and fast, >250 BPM.
    /// Occurs when blood volume is critically low.
    /// </summary>
    Thready = 5
}
