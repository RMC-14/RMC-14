using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._CM14.Xenos.Leap;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class XenoLeapingComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityCoordinates Origin;

    [DataField, AutoNetworkedField]
    public TimeSpan ParalyzeTime;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? HitSound;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan LeapEndTime;
}
