using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Body;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true), AutoGenerateComponentPause]
[Access(typeof(RMCRespiratorSystem))]
public sealed partial class RMCRespiratorComponent : Component
{
    /// <summary>
    ///     Suffocation number.
    /// </summary>
    /// <remarks>If number is positive, entity can't breathe.</remarks>
    [DataField, AutoNetworkedField]
    public float LoseBreath;

    [DataField, AutoNetworkedField]
    public FixedPoint2 BreathHealAmount = 2;

    /// <summary>
    ///     if(life_tick % 3 == 0)
    ///     One breath every 3 life_ticks. One life_tick takes 2 seconds.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan BreathInterval = TimeSpan.FromSeconds(6);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextBreathAt;
}
