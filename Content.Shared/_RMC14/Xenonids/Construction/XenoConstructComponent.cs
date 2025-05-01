using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Construction;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedXenoConstructionSystem))]
public sealed partial class XenoConstructComponent : Component;
