using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Sweep;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoTailSweepSystem))]
public sealed partial class XenoSweepingComponent : Component
{
    [DataField, AutoNetworkedField]
    public Direction? LastDirection;

    [DataField, AutoNetworkedField]
    public TimeSpan Delay = TimeSpan.FromSeconds(0.07);

    [DataField, AutoNetworkedField]
    public TimeSpan NextRotation;

    [DataField, AutoNetworkedField]
    public int TotalRotations;

    [DataField, AutoNetworkedField]
    public int MaxRotations = 4;
}
