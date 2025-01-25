using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids;

[RegisterComponent, NetworkedComponent]
[Access(typeof(XenoSystem))]
public sealed partial class XenoHeavyComponent : Component;