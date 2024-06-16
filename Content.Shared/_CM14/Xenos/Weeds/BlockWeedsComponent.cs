using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Xenos.Weeds;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedXenoWeedsSystem))]
public sealed partial class BlockWeedsComponent : Component;
