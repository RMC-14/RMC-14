using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Rotate;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoRotateSystem))]
public sealed partial class XenoRotateComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool FirstRotation = true;

    [DataField, AutoNetworkedField]
    public Direction TargetDirection;

    [DataField, AutoNetworkedField]
    public Direction? OriginalDirection;

    [DataField, AutoNetworkedField]
    public TimeSpan Delay = TimeSpan.FromSeconds(0.4);

    [DataField, AutoNetworkedField]
    public TimeSpan NextRotation;
}
