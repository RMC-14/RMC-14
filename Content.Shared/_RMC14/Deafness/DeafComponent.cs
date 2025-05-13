using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Deafness;

/// <summary>
///     Having this component will make clients game volume 0. Will also prevent them from hearing chat.
/// </summary>
[Access(typeof(SharedDeafnessSystem))]
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class DeafComponent : Component
{
    /// <summary>
    ///     The chance of a mob hearing a message sent in chat while they are deaf.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float HearChance = 0.4f;

    /// <summary>
    ///     Time it takes for the audio to fully fade out.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan FadeOutDelay = TimeSpan.FromSeconds(2);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan FadeOutEndAt;
}
