using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Smoke;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(RMCSmokeFadeSystem))]
public sealed partial class RMCSmokeFadeComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Alpha = 100f / 255f;

    [DataField]
    public float FadeFraction = 0.2f;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan FadeStart;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan FadeEnd;
}
