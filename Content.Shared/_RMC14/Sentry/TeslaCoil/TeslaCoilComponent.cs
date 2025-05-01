using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Sentry.TeslaCoil;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(TeslaCoilSystem))]
public sealed partial class RMCTeslaCoilComponent : Component
{
    /// <summary>
    /// Time when the coil last fired. Used for cooldown.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan LastFired;

    /// <summary>
    /// Delay between firing sequences.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan FireDelay = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Maximum range the coil can target entities.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Range = 3f;

    /// <summary>
    /// Maximum number of targets the coil can hit simultaneously per firing sequence.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MaxTargets = 5;

    /// <summary>
    /// Visual effect prototype for the beam from the coil to the target.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId TeslaBeamProto = "EffectTeslaBeam";

    /// <summary>
    /// Duration of the Stun (paralysis) effect. If TimeSpan.Zero, stun is not applied.
    /// Note: Paralysis might not apply to targets above a certain size (e.g., Xeno).
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan StunDuration = TimeSpan.FromSeconds(0);

    /// <summary>
    /// Duration of the Daze effect. If TimeSpan.Zero, daze is not applied.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan DazeDuration = TimeSpan.FromSeconds(8);

    /// <summary>
    /// Duration of the Slowdown (superslow) effect. If TimeSpan.Zero, slow is not applied.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan SlowDuration = TimeSpan.FromSeconds(4);
}
