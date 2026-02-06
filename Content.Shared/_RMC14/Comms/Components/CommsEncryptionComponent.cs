using Content.Shared._RMC14.Marines;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Comms;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CommsEncryptionComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Clarity = 1.0f; // 1.0 = 100% clear, 0.45 = 45% clear

    [DataField, AutoNetworkedField]
    public TimeSpan LastDecryptionTime;

    [DataField, AutoNetworkedField]
    public string ChallengePhrase = "PONG";

    [DataField, AutoNetworkedField]
    public bool IsGroundside;

    [DataField, AutoNetworkedField]
    public bool HasGracePeriod;

    [DataField, AutoNetworkedField]
    public TimeSpan GracePeriodEnd;

    [DataField, AutoNetworkedField]
    public TimeSpan DegradationStartTime;

    /// <summary>
    /// Time in seconds between clarity degradation ticks
    /// </summary>
    [DataField]
    public float DegradationInterval = 30f;

    /// <summary>
    /// Amount of clarity lost per degradation tick (2.5% = 0.025)
    /// </summary>
    [DataField]
    public float DegradationAmount = 0.025f;

    /// <summary>
    /// Minimum clarity before garbling (45% = 0.45)
    /// </summary>
    [DataField]
    public float MinClarity = 0.45f;

    /// <summary>
    /// Maximum clarity (95% = 0.95 for restoration)
    /// </summary>
    [DataField]
    public float MaxClarity = 0.95f;

    /// <summary>
    /// Grace period duration after decoding (1 minute)
    /// </summary>
    [DataField]
    public TimeSpan GracePeriodDuration = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Time from max to min clarity degradation (10 minutes)
    /// </summary>
    [DataField]
    public TimeSpan FullDegradationTime = TimeSpan.FromMinutes(10);
}
