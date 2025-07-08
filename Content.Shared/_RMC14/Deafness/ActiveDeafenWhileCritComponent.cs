using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Deafness;

/// <summary>
///     Added to the mob when they're crit
/// </summary>
[Access(typeof(SharedDeafnessSystem))]
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class ActiveDeafenWhileCritComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan Add = TimeSpan.FromSeconds(4);

    [DataField, AutoNetworkedField]
    public TimeSpan Every = TimeSpan.FromSeconds(2);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan AddAt = TimeSpan.Zero;
}
