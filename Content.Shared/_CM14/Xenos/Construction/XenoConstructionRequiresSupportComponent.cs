using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Xenos.Construction;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedXenoConstructionSystem))]
public sealed partial class XenoConstructionRequiresSupportComponent : Component;
