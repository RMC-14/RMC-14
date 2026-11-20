using Content.Shared._RMC14.Xenonids.Parasite;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.JoinXeno;

[RegisterComponent, NetworkedComponent]
[Access(typeof(LarvaQueueSystem), typeof(SharedXenoParasiteSystem))]
public sealed partial class LarvaQueuedComponent : Component;
