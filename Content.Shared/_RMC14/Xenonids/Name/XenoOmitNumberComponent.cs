using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Name;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedXenoNameSystem))]
public sealed partial class XenoOmitNumberComponent : Component;
