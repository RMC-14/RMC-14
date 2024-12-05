using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Finesse;

[RegisterComponent, NetworkedComponent]
[Access(typeof(XenoFinesseSystem))]
public sealed partial class XenoFinesseComponent : Component
{
    [DataField]
    public TimeSpan MarkedTime = TimeSpan.FromSeconds(3.5);
}
