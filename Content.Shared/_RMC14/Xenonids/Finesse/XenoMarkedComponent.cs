using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Finesse;

[RegisterComponent, NetworkedComponent]
[Access(typeof(XenoFinesseSystem))]
public sealed partial class XenoMarkedComponent : Component
{
    [DataField]
    public TimeSpan TimeAdded;

    [DataField]
    public TimeSpan WearOffAt;
}
