using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Xenonids.Construction;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedXenoConstructionSystem))]
public sealed partial class XenoConstructionSupportComponent : Component;
