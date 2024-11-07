using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Acid;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedXenoAcidSystem))]
public sealed partial class XenoAcidComponent : Component;
