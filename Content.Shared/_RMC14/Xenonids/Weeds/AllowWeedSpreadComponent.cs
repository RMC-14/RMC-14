using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Weeds;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedXenoWeedsSystem))]
public sealed partial class AllowWeedSpreadComponent : Component;
