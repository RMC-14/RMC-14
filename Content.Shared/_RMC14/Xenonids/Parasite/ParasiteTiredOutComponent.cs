using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Parasite;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedXenoParasiteSystem))]

public sealed partial class ParasiteTiredOutComponent : Component;
