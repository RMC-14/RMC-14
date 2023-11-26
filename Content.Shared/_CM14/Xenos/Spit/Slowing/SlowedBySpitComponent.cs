using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._CM14.Xenos.Spit.Slowing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedXenoSpitSystem))]
public sealed partial class SlowedBySpitComponent : Component
{
    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public float Multiplier = 0.5f;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan ExpiresAt;
}
