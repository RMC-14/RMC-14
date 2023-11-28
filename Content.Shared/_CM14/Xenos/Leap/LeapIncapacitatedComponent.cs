using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Xenos.Leap;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedXenoLeapSystem))]
public sealed partial class LeapIncapacitatedComponent : Component
{
    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan RecoverAt;
}
