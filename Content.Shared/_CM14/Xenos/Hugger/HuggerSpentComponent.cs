using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Xenos.Hugger;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedXenoHuggerSystem))]
public sealed partial class HuggerSpentComponent : Component
{
}
