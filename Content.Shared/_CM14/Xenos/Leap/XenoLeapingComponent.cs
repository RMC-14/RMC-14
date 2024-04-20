using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._CM14.Xenos.Leap;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoLeapingComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan KnockdownTime;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? HitSound;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan LeapEndTime;
}
